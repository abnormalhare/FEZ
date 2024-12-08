using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class RotatingGroupsHost : GameComponent, IRotatingGroupService, IScriptingBase
{
	private class RotatingGroupState
	{
		private const float SpinTime = 0.75f;

		public readonly TrileGroup Group;

		private readonly Vector3 Center;

		private readonly ArtObjectInstance[] AttachedArtObjects;

		private readonly Vector3[] AttachedAoOrigins;

		private readonly Quaternion[] AttachedAoRotations;

		private readonly TrileMaterializer[] CachedMaterializers;

		private readonly List<TrileInstance> TopLayer = new List<TrileInstance>();

		private readonly HashSet<Point> RecullAtPoints = new HashSet<Point>();

		public bool Enabled;

		public float SinceChanged;

		private SoundEffect sSpin;

		private Vector3 OriginalForward;

		private Vector4[] OriginalStates;

		private Vector3 OriginalPlayerPosition;

		private int SpinSign;

		private int Turns;

		private bool HeldOnto;

		private bool GroundedOn;

		public SpinAction Action { get; private set; }

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public ILevelMaterializer LevelMaterializer { private get; set; }

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IContentManagerProvider CMProvider { private get; set; }

		public RotatingGroupState(TrileGroup group)
		{
			ServiceHelper.InjectServices(this);
			Group = group;
			AttachedArtObjects = LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ActorSettings.AttachedGroup == Group.Id).ToArray();
			AttachedAoOrigins = AttachedArtObjects.Select((ArtObjectInstance x) => x.Position).ToArray();
			AttachedAoRotations = AttachedArtObjects.Select((ArtObjectInstance x) => x.Rotation).ToArray();
			CachedMaterializers = group.Triles.Select((TrileInstance x) => LevelMaterializer.GetTrileMaterializer(x.Trile)).ToArray();
			foreach (TrileInstance trile in Group.Triles)
			{
				trile.ForceSeeThrough = true;
				trile.Unsafe = true;
			}
			float num = Group.Triles.Where((TrileInstance x) => !x.Trile.Immaterial).Max((TrileInstance x) => x.Position.Y);
			foreach (TrileInstance trile2 in Group.Triles)
			{
				if (trile2.Position.Y == num)
				{
					TopLayer.Add(trile2);
				}
			}
			if (Group.SpinCenter != Vector3.Zero)
			{
				Center = Group.SpinCenter;
			}
			else
			{
				foreach (TrileInstance trile3 in Group.Triles)
				{
					Center += trile3.Position + FezMath.HalfVector;
				}
				Center /= (float)Group.Triles.Count;
			}
			Enabled = !Group.SpinNeedsTriggering;
			SinceChanged = 0f - Group.SpinOffset;
			if (SinceChanged != 0f)
			{
				SinceChanged -= 0.375f;
			}
			if (!string.IsNullOrEmpty(group.AssociatedSound))
			{
				try
				{
					sSpin = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/" + group.AssociatedSound);
				}
				catch (Exception)
				{
					Logger.Log("RotatingGroups", LogSeverity.Warning, "Could not find associated sound '" + group.AssociatedSound + "'");
				}
			}
		}

		public void Update(float elapsedSeconds)
		{
			if (!Enabled && Action == SpinAction.Idle)
			{
				return;
			}
			SinceChanged += elapsedSeconds;
			float spinFrequency = Group.SpinFrequency;
			switch (Action)
			{
			case SpinAction.Idle:
				if (SinceChanged >= spinFrequency)
				{
					SinceChanged -= spinFrequency;
					Rotate(Group.SpinClockwise, (!Group.Spin180Degrees) ? 1 : 2);
				}
				break;
			case SpinAction.Spinning:
			{
				float num = Easing.EaseInOut(FezMath.Saturate(FezMath.Saturate(SinceChanged / (0.75f * (float)Turns)) / 0.75f), EasingType.Quartic, EasingType.Quadratic);
				float num2 = num * ((float)Math.PI / 2f) * (float)SpinSign * (float)Turns;
				Matrix matrix = Matrix.CreateFromAxisAngle(Vector3.UnitY, num2);
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num2);
				if (!PlayerManager.IsOnRotato && num < 0.5f)
				{
					HeldOnto |= Group.Triles.Contains(PlayerManager.HeldInstance);
					GroundedOn |= PlayerManager.Grounded && TopLayer.Contains(PlayerManager.Ground.First);
					if (GroundedOn || HeldOnto)
					{
						OriginalPlayerPosition = PlayerManager.Position;
						PlayerManager.IsOnRotato = true;
					}
				}
				if ((GroundedOn || HeldOnto) && num > 0.1f && !CameraManager.ForceTransition)
				{
					if (HeldOnto && Group.FallOnRotate)
					{
						PlayerManager.Action = ActionType.Idle;
						PlayerManager.HeldInstance = null;
						PlayerManager.IsOnRotato = false;
						HeldOnto = false;
					}
					else
					{
						CameraManager.ForceTransition = true;
						CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(SpinSign * Turns), -1f);
						CameraManager.Direction = -CameraManager.LastViewpoint.ForwardVector();
						CameraManager.RebuildView();
					}
				}
				Vector3 vector = new Vector3(Center.X - 0.5f, Center.Y - 0.5f, Center.Z - 0.5f);
				for (int i = 0; i < OriginalStates.Length; i++)
				{
					TrileInstance trileInstance = Group.Triles[i];
					Vector4 vector2 = OriginalStates[i];
					Vector3 vector3 = Vector3.Transform(new Vector3(vector2.X, vector2.Y, vector2.Z), matrix);
					trileInstance.Position = new Vector3(vector3.X + vector.X, vector3.Y + vector.Y, vector3.Z + vector.Z);
					trileInstance.SetPhiLight(vector2.W + num2);
					CachedMaterializers[i].UpdateInstance(trileInstance);
				}
				for (int j = 0; j < AttachedArtObjects.Length; j++)
				{
					AttachedArtObjects[j].Rotation = AttachedAoRotations[j] * quaternion;
					AttachedArtObjects[j].Position = Vector3.Transform(AttachedAoOrigins[j] - Center, matrix) + Center;
				}
				if (GroundedOn || HeldOnto)
				{
					Vector3 position = PlayerManager.Position;
					Vector3 vector4 = Vector3.Transform(OriginalPlayerPosition - Center, matrix) + Center;
					if (!HeldOnto || !Group.FallOnRotate)
					{
						CameraManager.Center += vector4 - position;
						CameraManager.Direction = Vector3.Transform(-OriginalForward, matrix);
					}
					PlayerManager.Position += vector4 - position;
				}
				if (SinceChanged >= 0.75f * (float)Turns)
				{
					if (GroundedOn || HeldOnto)
					{
						PlayerManager.IsOnRotato = false;
						RotateTriles();
						CameraManager.ForceTransition = false;
						PlayerManager.ForceOverlapsDetermination();
					}
					else
					{
						RotateTriles();
					}
					SinceChanged -= 0.75f;
					Action = SpinAction.Idle;
				}
				break;
			}
			}
		}

		public void Rotate(bool clockwise, int turns)
		{
			SpinSign = (clockwise ? 1 : (-1));
			Turns = turns;
			foreach (TrileInstance trile in Group.Triles)
			{
				LevelMaterializer.UnregisterViewedInstance(trile);
				if (trile.InstanceId == -1)
				{
					LevelMaterializer.CullInstanceInNoRegister(trile);
				}
				trile.SkipCulling = true;
			}
			LevelMaterializer.CommitBatchesIfNeeded();
			RecordStates();
			for (int i = 0; i < AttachedArtObjects.Length; i++)
			{
				AttachedAoRotations[i] = AttachedArtObjects[i].Rotation;
				AttachedAoOrigins[i] = AttachedArtObjects[i].Position;
			}
			HeldOnto = Group.Triles.Contains(PlayerManager.HeldInstance);
			GroundedOn = PlayerManager.Grounded && TopLayer.Contains(PlayerManager.Ground.First);
			if (GroundedOn || HeldOnto)
			{
				PlayerManager.IsOnRotato = true;
			}
			OriginalForward = CameraManager.Viewpoint.ForwardVector();
			OriginalPlayerPosition = PlayerManager.Position;
			Action = SpinAction.Spinning;
			if (sSpin != null)
			{
				sSpin.EmitAt(Center, loop: false, RandomHelper.Centered(0.10000000149011612), paused: false).FadeDistance = 50f;
			}
		}

		private void RecordStates()
		{
			OriginalStates = Group.Triles.Select((TrileInstance x) => new Vector4(x.Position + FezMath.HalfVector - Center, x.Phi)).ToArray();
		}

		private void RotateTriles()
		{
			float num = (float)Math.PI / 2f * (float)SpinSign * (float)Turns;
			Matrix matrix = Matrix.CreateFromAxisAngle(Vector3.UnitY, num);
			RecullAtPoints.Clear();
			bool flag = CameraManager.Viewpoint.SideMask().X != 0f;
			Vector3 vector = new Vector3(Center.X - 0.5f, Center.Y - 0.5f, Center.Z - 0.5f);
			for (int i = 0; i < OriginalStates.Length; i++)
			{
				TrileInstance trileInstance = Group.Triles[i];
				Vector4 vector2 = OriginalStates[i];
				trileInstance.Position = Vector3.Transform(vector2.XYZ(), matrix) + vector;
				trileInstance.Phi = FezMath.WrapAngle(vector2.W + num);
				LevelManager.UpdateInstance(trileInstance);
				trileInstance.SkipCulling = false;
				RecullAtPoints.Add(new Point(flag ? trileInstance.Emplacement.X : trileInstance.Emplacement.Z, trileInstance.Emplacement.Y));
			}
			foreach (Point recullAtPoint in RecullAtPoints)
			{
				LevelManager.RecullAt(recullAtPoint, skipCommit: true);
			}
			LevelMaterializer.CommitBatchesIfNeeded();
		}
	}

	private readonly List<RotatingGroupState> RotatingGroups = new List<RotatingGroupState>();

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	public RotatingGroupsHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		TryInitialize();
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		RotatingGroups.Clear();
		RotatingGroups.AddRange(from x in LevelManager.Groups.Values
			where x.ActorType == ActorType.RotatingGroup
			select new RotatingGroupState(x));
		base.Enabled = RotatingGroups.Count > 0;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || GameState.InMenuCube || GameState.InFpsMode)
		{
			return;
		}
		float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
		foreach (RotatingGroupState rotatingGroup in RotatingGroups)
		{
			rotatingGroup.Update(elapsedSeconds);
		}
	}

	public void ResetEvents()
	{
	}

	public void Rotate(int id, bool clockwise, int turns)
	{
		if (!base.Enabled)
		{
			return;
		}
		foreach (RotatingGroupState rotatingGroup in RotatingGroups)
		{
			if (rotatingGroup.Group.Id == id)
			{
				RotatingGroupState cached = rotatingGroup;
				Waiters.Wait(() => cached.Action == SpinAction.Idle, delegate
				{
					cached.Rotate(clockwise, turns);
					cached.SinceChanged = 0f;
				});
			}
		}
	}

	public void SetEnabled(int id, bool enabled)
	{
		if (!base.Enabled)
		{
			return;
		}
		foreach (RotatingGroupState rotatingGroup in RotatingGroups)
		{
			if (rotatingGroup.Group.Id == id)
			{
				rotatingGroup.Enabled = enabled;
				rotatingGroup.SinceChanged = 0f;
			}
		}
	}
}
