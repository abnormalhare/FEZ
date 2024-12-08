using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class SuckBlocksHost : GameComponent
{
	private class SuckBlockState
	{
		public static readonly TimeSpan ProcessingTime = TimeSpan.FromSeconds(0.5);

		public readonly TrileInstance Instance;

		public readonly TrileGroup Group;

		private SuckBlockAction action;

		public TimeSpan SinceActionChanged { get; private set; }

		public SuckBlockAction Action
		{
			get
			{
				return action;
			}
			set
			{
				if (action != value)
				{
					SinceActionChanged = TimeSpan.Zero;
				}
				action = value;
			}
		}

		public SuckBlockState(TrileInstance instance, TrileGroup group)
		{
			Instance = instance;
			Group = group;
		}

		public void Update(TimeSpan elapsed)
		{
			SinceActionChanged += elapsed;
		}
	}

	private enum SuckBlockAction
	{
		Idle,
		Processing,
		Sucking,
		Rejected,
		Accepted
	}

	private readonly List<SuckBlockState> TrackedSuckBlocks = new List<SuckBlockState>();

	private readonly List<Volume> HostingVolumes = new List<Volume>();

	private SoundEmitter eCratePush;

	private SoundEmitter eSuck;

	private SoundEffect sDenied;

	private SoundEffect sSuck;

	private SoundEffect[] sAccept;

	private List<BackgroundPlane> highlightPlanes;

	private readonly Ray[] cornerRays = new Ray[4];

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ISuckBlockService SuckBlockService { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public SuckBlocksHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += InitSuckBlocks;
		if (LevelManager.Name != null)
		{
			InitSuckBlocks();
		}
		sAccept = new SoundEffect[4]
		{
			CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/AcceptSuckBlock1"),
			CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/AcceptSuckBlock2"),
			CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/AcceptSuckBlock3"),
			CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/AcceptSuckBlock4")
		};
		sDenied = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/Denied");
		sSuck = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/SuckBlockSuck");
	}

	private void InitSuckBlocks()
	{
		HostingVolumes.Clear();
		TrackedSuckBlocks.Clear();
		highlightPlanes = null;
		eCratePush = (eSuck = null);
		foreach (TrileGroup value in LevelManager.Groups.Values)
		{
			if (value.ActorType == ActorType.SuckBlock)
			{
				TrileInstance trileInstance = value.Triles.First();
				if (trileInstance.ActorSettings.HostVolume.HasValue)
				{
					TrackedSuckBlocks.Add(new SuckBlockState(trileInstance, value));
					EnableTrile(trileInstance);
					HostingVolumes.Add(LevelManager.Volumes[trileInstance.ActorSettings.HostVolume.Value]);
				}
			}
		}
		if (TrackedSuckBlocks.Count > 0)
		{
			eCratePush = CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/PushPickup").EmitAt(Vector3.Zero, loop: true, paused: true);
			highlightPlanes = new List<BackgroundPlane>();
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || GameState.Loading || TrackedSuckBlocks.Count == 0)
		{
			return;
		}
		FaceOrientation visibleOrientation = CameraManager.VisibleOrientation;
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		Vector3 vector2 = CameraManager.Viewpoint.DepthMask();
		Vector3 vector3 = CameraManager.Viewpoint.ScreenSpaceMask();
		Vector3 vector4 = vector3 / 2f;
		bool flag = false;
		foreach (SuckBlockState trackedSuckBlock in TrackedSuckBlocks)
		{
			TrileInstance instance = trackedSuckBlock.Instance;
			if (PlayerManager.HeldInstance == instance)
			{
				continue;
			}
			int value = instance.ActorSettings.HostVolume.Value;
			Vector3 vector5 = instance.Center * (Vector3.One - vector2) + CameraManager.Position * vector2;
			cornerRays[0] = new Ray
			{
				Position = vector5 + vector4 * new Vector3(1f, 0.499f, 1f),
				Direction = vector
			};
			cornerRays[1] = new Ray
			{
				Position = vector5 + vector4 * new Vector3(1f, -1f, 1f),
				Direction = vector
			};
			cornerRays[2] = new Ray
			{
				Position = vector5 + vector4 * new Vector3(-1f, 0.499f, -1f),
				Direction = vector
			};
			cornerRays[3] = new Ray
			{
				Position = vector5 + vector4 * new Vector3(-1f, -1f, -1f),
				Direction = vector
			};
			trackedSuckBlock.Update(gameTime.ElapsedGameTime);
			eCratePush.Position = instance.Center;
			bool flag2 = false;
			foreach (Volume hostingVolume in HostingVolumes)
			{
				if (!hostingVolume.Orientations.Contains(visibleOrientation))
				{
					continue;
				}
				bool flag3 = false;
				Ray[] array = cornerRays;
				foreach (Ray ray in array)
				{
					flag3 |= hostingVolume.BoundingBox.Intersects(ray).HasValue;
				}
				if (!flag3)
				{
					continue;
				}
				flag2 = true;
				if (trackedSuckBlock.Action == SuckBlockAction.Sucking && (eSuck == null || eSuck.Dead))
				{
					eSuck = sSuck.EmitAt(instance.Center, loop: true);
				}
				flag |= trackedSuckBlock.Action == SuckBlockAction.Sucking || trackedSuckBlock.Action == SuckBlockAction.Processing;
				Vector3 vector6 = (hostingVolume.BoundingBox.Min + hostingVolume.BoundingBox.Max) / 2f;
				Vector3 vector7 = (vector6 - instance.Center) * vector3;
				float num = vector7.Length();
				if (num < 0.01f)
				{
					if (trackedSuckBlock.Action == SuckBlockAction.Sucking)
					{
						trackedSuckBlock.Action = SuckBlockAction.Processing;
						PlayerManager.CanRotate = false;
						eCratePush.VolumeFactor = 0.5f;
						eCratePush.Cue.Pitch = -0.4f;
					}
					if (trackedSuckBlock.Action == SuckBlockAction.Processing)
					{
						Vector3 vector8 = (hostingVolume.BoundingBox.Max - hostingVolume.BoundingBox.Min) / 2f;
						Vector3 vector9 = hostingVolume.BoundingBox.Min * vector3 + vector6 * vector2 + vector8 * vector - vector * 0.5f - vector2 * 0.5f;
						Vector3 value2 = vector9 - vector;
						instance.Position = Vector3.Lerp(value2, vector9, (float)trackedSuckBlock.SinceActionChanged.Ticks / (float)SuckBlockState.ProcessingTime.Ticks);
						LevelManager.UpdateInstance(instance);
						if (trackedSuckBlock.SinceActionChanged > SuckBlockState.ProcessingTime)
						{
							PlayerManager.CanRotate = true;
							if (hostingVolume.Id == value)
							{
								DisableTrile(instance);
								trackedSuckBlock.Action = SuckBlockAction.Accepted;
								if (eCratePush.Cue.State != SoundState.Paused)
								{
									eCratePush.Cue.Pause();
								}
								SuckBlockService.OnSuck(trackedSuckBlock.Group.Id);
								sAccept[4 - TrackedSuckBlocks.Count].Emit();
								string text = instance.Trile.CubemapPath.Substring(instance.Trile.CubemapPath.Length - 1).ToLower(CultureInfo.InvariantCulture);
								Texture2D texture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/suck_blocks/four_highlight_" + text);
								BackgroundPlane plane = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, texture)
								{
									Position = instance.Center + visibleOrientation.AsVector() * (17f / 32f),
									Rotation = CameraManager.Rotation,
									Doublesided = true,
									Fullbright = true,
									Opacity = 0f
								};
								List<BackgroundPlane> localPlanes = highlightPlanes;
								highlightPlanes.Add(plane);
								LevelManager.AddPlane(plane);
								Waiters.Interpolate(1.0, delegate(float s)
								{
									plane.Opacity = s;
								});
								if (TrackedSuckBlocks.Count == 1)
								{
									Waiters.Wait(2.0, delegate
									{
										Waiters.Interpolate(1.0, delegate(float s)
										{
											foreach (BackgroundPlane item in localPlanes)
											{
												item.Opacity = 1f - s;
											}
										}, delegate
										{
											eSuck = null;
										});
									});
									eSuck.FadeOutAndDie(1f);
								}
							}
							else
							{
								trackedSuckBlock.Action = SuckBlockAction.Rejected;
							}
						}
					}
					if (trackedSuckBlock.Action == SuckBlockAction.Rejected && instance.PhysicsState.Velocity.XZ() == Vector2.Zero)
					{
						int num2 = ((!RandomHelper.Probability(0.5)) ? 1 : (-1));
						Vector3 vector10 = new Vector3(num2, 0.75f, num2) * vector3;
						ServiceHelper.AddComponent(new CamShake(base.Game)
						{
							Distance = 0.1f,
							Duration = TimeSpan.FromSeconds(0.25)
						});
						sDenied.Emit();
						if (eCratePush.Cue.State != SoundState.Paused)
						{
							eCratePush.Cue.Pause();
						}
						instance.PhysicsState.Velocity += 6f * vector10 * (float)gameTime.ElapsedGameTime.TotalSeconds;
					}
				}
				else if (trackedSuckBlock.Action != SuckBlockAction.Rejected)
				{
					if (instance.PhysicsState.Grounded && eCratePush.Cue.State != 0)
					{
						eCratePush.Cue.Pitch = 0f;
						eCratePush.Cue.Resume();
					}
					else if (!instance.PhysicsState.Grounded && eCratePush.Cue.State != SoundState.Paused)
					{
						eCratePush.Cue.Pause();
					}
					if (eCratePush.Cue.State == SoundState.Playing)
					{
						eCratePush.VolumeFactor = FezMath.Saturate(Math.Abs(instance.PhysicsState.Velocity.Dot(FezMath.XZMask) / 0.1f));
					}
					trackedSuckBlock.Action = SuckBlockAction.Sucking;
					instance.PhysicsState.Velocity += 0.25f * (vector7 / num) * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
			}
			if (!flag2)
			{
				trackedSuckBlock.Action = SuckBlockAction.Idle;
			}
		}
		if (!flag && eSuck != null && !eSuck.Dead)
		{
			eSuck.FadeOutAndDie(0.1f);
			eSuck = null;
		}
		for (int j = 0; j < TrackedSuckBlocks.Count; j++)
		{
			if (TrackedSuckBlocks[j].Action == SuckBlockAction.Accepted)
			{
				TrackedSuckBlocks.RemoveAt(j);
				j--;
			}
		}
	}

	private static void DisableTrile(TrileInstance instance)
	{
		Trile trile = instance.Trile;
		trile.ActorSettings.Type = ActorType.None;
		Dictionary<FaceOrientation, CollisionType> faces = trile.Faces;
		Dictionary<FaceOrientation, CollisionType> faces2 = trile.Faces;
		Dictionary<FaceOrientation, CollisionType> faces3 = trile.Faces;
		CollisionType collisionType2 = (trile.Faces[FaceOrientation.Front] = CollisionType.None);
		CollisionType collisionType4 = (faces3[FaceOrientation.Back] = collisionType2);
		CollisionType value = (faces2[FaceOrientation.Right] = collisionType4);
		faces[FaceOrientation.Left] = value;
	}

	private static void EnableTrile(TrileInstance instance)
	{
		Trile trile = instance.Trile;
		trile.ActorSettings.Type = ActorType.SinkPickup;
		Dictionary<FaceOrientation, CollisionType> faces = trile.Faces;
		Dictionary<FaceOrientation, CollisionType> faces2 = trile.Faces;
		Dictionary<FaceOrientation, CollisionType> faces3 = trile.Faces;
		CollisionType collisionType2 = (trile.Faces[FaceOrientation.Front] = CollisionType.AllSides);
		CollisionType collisionType4 = (faces3[FaceOrientation.Back] = collisionType2);
		CollisionType value = (faces2[FaceOrientation.Right] = collisionType4);
		faces[FaceOrientation.Left] = value;
	}
}
