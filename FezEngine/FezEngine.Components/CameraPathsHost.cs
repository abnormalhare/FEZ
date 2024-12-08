using System;
using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Components;

public class CameraPathsHost : GameComponent
{
	private class CameraPathState
	{
		private readonly MovementPath Path;

		private readonly List<PathSegment> Nodes;

		private TimeSpan sinceSegmentStarted;

		private int nodeIndex;

		private Viewpoint originalViewpoint;

		private Viewpoint firstNodeViewpoint;

		private float originalPixelsPerTrixel;

		private Vector3 originalCenter;

		private Vector3 originalDirection;

		private float originalRadius;

		private bool justStarted;

		private bool Enabled { get; set; }

		private PathSegment CurrentNode => Nodes[nodeIndex];

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IDebuggingBag DebuggingBag { private get; set; }

		[ServiceDependency]
		public IContentManagerProvider CMProvider { private get; set; }

		public CameraPathState(MovementPath path)
		{
			ServiceHelper.InjectServices(this);
			Path = path;
			Nodes = path.Segments;
			Enabled = true;
			foreach (PathSegment node in Nodes)
			{
				CameraNodeData cameraNodeData = node.CustomData as CameraNodeData;
				if (cameraNodeData.SoundName != null)
				{
					node.Sound = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/" + cameraNodeData.SoundName);
				}
			}
			Reset();
			StartNewSegment();
		}

		private void Reset()
		{
			nodeIndex = 1;
			sinceSegmentStarted = TimeSpan.Zero;
			justStarted = true;
		}

		public void Update(TimeSpan elapsed)
		{
			if (!Enabled || Path.NeedsTrigger)
			{
				return;
			}
			if (justStarted)
			{
				originalViewpoint = CameraManager.Viewpoint;
				originalCenter = CameraManager.Center;
				originalDirection = CameraManager.Direction;
				originalPixelsPerTrixel = CameraManager.PixelsPerTrixel;
				originalRadius = CameraManager.Radius;
				bool perspective = (Nodes[0].CustomData as CameraNodeData).Perspective;
				if (Path.InTransition)
				{
					nodeIndex = 1;
					Nodes.Insert(0, new PathSegment
					{
						Destination = originalCenter,
						Orientation = Quaternion.Inverse(CameraManager.Rotation),
						CustomData = new CameraNodeData
						{
							PixelsPerTrixel = (int)originalPixelsPerTrixel,
							Perspective = perspective
						}
					});
				}
				if (Path.OutTransition)
				{
					Nodes.Add(new PathSegment
					{
						Destination = originalCenter,
						Orientation = Quaternion.Inverse(CameraManager.Rotation),
						CustomData = new CameraNodeData
						{
							PixelsPerTrixel = (int)originalPixelsPerTrixel,
							Perspective = perspective
						}
					});
				}
				if (Nodes.Count < 2)
				{
					EndPath();
					return;
				}
				CameraNodeData cameraNodeData = Nodes[0].CustomData as CameraNodeData;
				firstNodeViewpoint = FezMath.OrientationFromDirection(Vector3.Transform(Vector3.Forward, Nodes[0].Orientation).MaxClampXZ()).AsViewpoint();
				Viewpoint view = (cameraNodeData.Perspective ? Viewpoint.Perspective : firstNodeViewpoint);
				CameraManager.ChangeViewpoint(view);
				if (cameraNodeData.Perspective)
				{
					CameraManager.Radius = 0.001f;
				}
				if (cameraNodeData.PixelsPerTrixel != 0 && CameraManager.PixelsPerTrixel != (float)cameraNodeData.PixelsPerTrixel)
				{
					CameraManager.PixelsPerTrixel = cameraNodeData.PixelsPerTrixel;
				}
				StartNewSegment();
				justStarted = false;
			}
			if (CameraManager.ActionRunning)
			{
				sinceSegmentStarted += elapsed;
			}
			if (sinceSegmentStarted >= CurrentNode.Duration + CurrentNode.WaitTimeOnFinish)
			{
				ChangeSegment();
			}
			if (!Enabled || Path.NeedsTrigger)
			{
				return;
			}
			float num = (float)FezMath.Saturate(sinceSegmentStarted.TotalSeconds / CurrentNode.Duration.TotalSeconds);
			float amount = ((CurrentNode.Deceleration == 0f && CurrentNode.Acceleration == 0f) ? num : ((CurrentNode.Acceleration == 0f) ? Easing.Ease(num, 0f - CurrentNode.Deceleration, EasingType.Quadratic) : ((CurrentNode.Deceleration != 0f) ? Easing.EaseInOut(num, EasingType.Sine, CurrentNode.Acceleration, EasingType.Sine, CurrentNode.Deceleration) : Easing.Ease(num, CurrentNode.Acceleration, EasingType.Quadratic))));
			PathSegment pathSegment = Nodes[Math.Max(nodeIndex - 1, 0)];
			PathSegment currentNode = CurrentNode;
			Vector3 center;
			Quaternion rotation;
			if (Path.IsSpline)
			{
				PathSegment pathSegment2 = Nodes[Math.Max(nodeIndex - 2, 0)];
				center = Vector3.CatmullRom(value4: Nodes[Math.Min(nodeIndex + 1, Nodes.Count - 1)].Destination, value1: pathSegment2.Destination, value2: pathSegment.Destination, value3: currentNode.Destination, amount: amount);
				rotation = Quaternion.Slerp(pathSegment.Orientation, currentNode.Orientation, amount);
			}
			else
			{
				center = Vector3.Lerp(pathSegment.Destination, currentNode.Destination, amount);
				rotation = Quaternion.Slerp(pathSegment.Orientation, currentNode.Orientation, amount);
			}
			float num2 = MathHelper.Lerp(pathSegment.JitterFactor, currentNode.JitterFactor, amount);
			if (num2 > 0f)
			{
				center += new Vector3(RandomHelper.Centered(num2) * 0.5f, RandomHelper.Centered(num2) * 0.5f, RandomHelper.Centered(num2) * 0.5f);
			}
			Vector3 direction = Vector3.Transform(Vector3.Forward, rotation);
			CameraNodeData cameraNodeData2 = pathSegment.CustomData as CameraNodeData;
			CameraNodeData cameraNodeData3 = currentNode.CustomData as CameraNodeData;
			if (!cameraNodeData3.Perspective)
			{
				float pixelsPerTrixel = MathHelper.Lerp((cameraNodeData2.PixelsPerTrixel == 0) ? originalPixelsPerTrixel : ((float)cameraNodeData2.PixelsPerTrixel), (cameraNodeData3.PixelsPerTrixel == 0) ? originalPixelsPerTrixel : ((float)cameraNodeData3.PixelsPerTrixel), amount);
				CameraManager.PixelsPerTrixel = pixelsPerTrixel;
			}
			Viewpoint viewpoint = (cameraNodeData3.Perspective ? Viewpoint.Perspective : firstNodeViewpoint);
			if (viewpoint != CameraManager.Viewpoint)
			{
				if (viewpoint == Viewpoint.Perspective)
				{
					CameraManager.Radius = 0.001f;
				}
				CameraManager.ChangeViewpoint(viewpoint);
			}
			CameraManager.Center = center;
			CameraManager.Direction = direction;
			if (cameraNodeData3.Perspective)
			{
				if (nodeIndex == 1)
				{
					CameraManager.Radius = MathHelper.Lerp(originalRadius, 0.001f, amount);
				}
				else if (nodeIndex == Nodes.Count - 1)
				{
					CameraManager.Radius = MathHelper.Lerp(0.001f, originalRadius, amount);
				}
				else
				{
					CameraManager.Radius = 0.001f;
				}
			}
		}

		private void ChangeSegment()
		{
			sinceSegmentStarted -= CurrentNode.Duration + CurrentNode.WaitTimeOnFinish;
			if (CurrentNode.Sound != null)
			{
				CurrentNode.Sound.EmitAt(CameraManager.Center, loop: false, RandomHelper.Centered(0.07500000298023224), paused: false);
			}
			nodeIndex++;
			if (nodeIndex == Nodes.Count || nodeIndex == -1)
			{
				EndPath();
			}
			if (Enabled && !Path.NeedsTrigger)
			{
				StartNewSegment();
			}
			if (Path.RunSingleSegment)
			{
				Path.NeedsTrigger = true;
				Path.RunSingleSegment = false;
			}
		}

		private void StartNewSegment()
		{
			sinceSegmentStarted -= CurrentNode.WaitTimeOnStart;
		}

		private void EndPath()
		{
			if (Path.InTransition)
			{
				Nodes.RemoveAt(0);
			}
			if (Path.OutTransition)
			{
				Nodes.RemoveAt(Nodes.Count - 1);
			}
			Path.NeedsTrigger = true;
			Path.RunOnce = false;
			CameraManager.ChangeViewpoint(originalViewpoint);
			Reset();
		}
	}

	private readonly List<CameraPathState> trackedPaths = new List<CameraPathState>();

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	public CameraPathsHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
	}

	private void TrackNewPaths()
	{
		trackedPaths.Clear();
		foreach (MovementPath value in LevelManager.Paths.Values)
		{
			trackedPaths.Add(new CameraPathState(value));
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TrackNewPaths;
	}

	public override void Update(GameTime gameTime)
	{
		if (EngineState.Loading || EngineState.TimePaused)
		{
			return;
		}
		if (trackedPaths.Count != LevelManager.Paths.Count)
		{
			TrackNewPaths();
		}
		if (trackedPaths.Count == 0)
		{
			return;
		}
		foreach (CameraPathState trackedPath in trackedPaths)
		{
			trackedPath.Update(gameTime.ElapsedGameTime);
		}
	}
}
