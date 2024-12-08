using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class TombstonesHost : DrawableGameComponent
{
	private class TombstoneState
	{
		private const float SpinTime = 0.75f;

		private readonly TombstonesHost Host;

		private SpinAction State;

		private TimeSpan SinceChanged;

		private int SpinSign;

		private Vector3 OriginalPlayerPosition;

		private Quaternion OriginalAoRotation;

		internal Viewpoint LastViewpoint;

		public readonly ArtObjectInstance ArtObject;

		[ServiceDependency]
		public IPhysicsManager PhysicsManager { private get; set; }

		[ServiceDependency]
		public IInputManager InputManager { private get; set; }

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public ITombstoneService TombstoneService { private get; set; }

		public TombstoneState(TombstonesHost host, ArtObjectInstance ao)
		{
			ServiceHelper.InjectServices(this);
			Host = host;
			ArtObject = ao;
			if (GameState.SaveData.ThisLevel.PivotRotations.TryGetValue(ArtObject.Id, out var value) && value != 0)
			{
				int num = Math.Abs(value);
				for (int i = 0; i < num; i++)
				{
					OriginalAoRotation = ArtObject.Rotation;
					float angle = (float)Math.PI / 2f * (float)Math.Sign(value);
					Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
					ArtObject.Rotation *= quaternion;
				}
			}
			LastViewpoint = FezMath.OrientationFromDirection(Vector3.Transform(Vector3.Forward, ArtObject.Rotation).MaxClampXZ()).AsViewpoint();
		}

		public bool Update(TimeSpan elapsed)
		{
			SinceChanged += elapsed;
			switch (State)
			{
			case SpinAction.Idle:
			{
				Vector3 vector = (PlayerManager.Position - (ArtObject.Position - new Vector3(0f, 1f, 0f))) * CameraManager.Viewpoint.ScreenSpaceMask();
				vector.X += vector.Z;
				Vector3 vector2 = vector.Abs();
				bool flag = vector2.X < 0.9f && vector2.Y < 1f;
				if (FezMath.AlmostEqual(Vector3.Transform(Vector3.UnitZ, ArtObject.Rotation).Abs(), CameraManager.Viewpoint.DepthMask()) && flag && PlayerManager.CarriedInstance == null && PlayerManager.Grounded && PlayerManager.Action != ActionType.GrabTombstone && InputManager.FpsToggle != FezButtonState.Pressed && InputManager.GrabThrow == FezButtonState.Pressed && PlayerManager.Action != ActionType.ReadingSign)
				{
					SinceChanged = TimeSpan.Zero;
					return true;
				}
				break;
			}
			case SpinAction.Grabbed:
				if (PlayerManager.Action != ActionType.GrabTombstone)
				{
					State = SpinAction.Idle;
				}
				break;
			case SpinAction.Spinning:
			{
				double num = FezMath.Saturate(SinceChanged.TotalSeconds / 0.75);
				float angle = Easing.EaseIn((float)((num < 0.949999988079071) ? (num / 0.949999988079071) : (1.0 + Math.Sin((num - 0.949999988079071) / 0.05000000074505806 * 6.2831854820251465 * 2.0) * 0.009999999776482582 * (1.0 - num) / 0.05000000074505806)), EasingType.Linear) * ((float)Math.PI / 2f) * (float)SpinSign;
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
				ArtObject.Rotation = OriginalAoRotation * quaternion;
				PlayerManager.Position = Vector3.Transform(OriginalPlayerPosition - ArtObject.Position, quaternion) + ArtObject.Position;
				if (SinceChanged.TotalSeconds >= 0.75)
				{
					LastViewpoint = FezMath.OrientationFromDirection(Vector3.Transform(Vector3.Forward, ArtObject.Rotation).MaxClampXZ()).AsViewpoint();
					int num2 = Host.TrackedStones.Count((TombstoneState x) => x.LastViewpoint == LastViewpoint);
					TombstoneService.UpdateAlignCount(num2);
					if (num2 > 1)
					{
						TombstoneService.OnMoreThanOneAligned();
					}
					Host.StopSkullRotations = num2 == 4;
					PlayerManager.Action = ActionType.GrabTombstone;
					PlayerManager.Position += 0.5f * Vector3.UnitY;
					PlayerManager.Velocity = Vector3.Down;
					PhysicsManager.Update(PlayerManager);
					SinceChanged -= TimeSpan.FromSeconds(0.75);
					State = SpinAction.Grabbed;
				}
				break;
			}
			}
			return false;
		}

		public void GrabOnto()
		{
			PlayerManager.Action = ActionType.GrabTombstone;
			Waiters.Wait(0.4, (float _) => PlayerManager.Action != ActionType.GrabTombstone, delegate
			{
				if (PlayerManager.Action == ActionType.GrabTombstone)
				{
					Host.GrabSound.EmitAt(ArtObject.Position);
					State = SpinAction.Grabbed;
				}
			});
		}

		public void TrySpin()
		{
			if (State != SpinAction.Grabbed)
			{
				return;
			}
			if (PlayerManager.Action != ActionType.GrabTombstone)
			{
				State = SpinAction.Idle;
			}
			else if (PlayerManager.Animation.Timing.Ended && CameraManager.Viewpoint != Viewpoint.Perspective && CameraManager.LastViewpoint != CameraManager.Viewpoint)
			{
				SpinSign = CameraManager.LastViewpoint.GetDistance(CameraManager.Viewpoint);
				if (SpinSign == 1)
				{
					Host.TurnRight.EmitAt(ArtObject.Position);
				}
				else
				{
					Host.TurnLeft.EmitAt(ArtObject.Position);
				}
				if (!GameState.SaveData.ThisLevel.PivotRotations.TryGetValue(ArtObject.Id, out var value))
				{
					GameState.SaveData.ThisLevel.PivotRotations.Add(ArtObject.Id, SpinSign);
				}
				else
				{
					GameState.SaveData.ThisLevel.PivotRotations[ArtObject.Id] = value + SpinSign;
				}
				PlayerManager.Position = PlayerManager.Position * CameraManager.LastViewpoint.ScreenSpaceMask() + ArtObject.Position * CameraManager.LastViewpoint.DepthMask() + -CameraManager.LastViewpoint.ForwardVector();
				OriginalPlayerPosition = PlayerManager.Position;
				OriginalAoRotation = ArtObject.Rotation;
				SinceChanged = TimeSpan.Zero;
				State = SpinAction.Spinning;
				PlayerManager.Action = ActionType.PivotTombstone;
			}
		}
	}

	private readonly List<TombstoneState> TrackedStones = new List<TombstoneState>();

	private ArtObjectInstance SkullAo;

	private Vector4[] SkullAttachedTrilesOriginalStates;

	private TrileInstance[] SkullTopLayer;

	private TrileInstance[] SkullAttachedTriles;

	private Quaternion InterpolatedRotation;

	private Quaternion OriginalRotation;

	private bool SkullRotates;

	private bool StopSkullRotations;

	private SoundEffect GrabSound;

	private SoundEffect TurnLeft;

	private SoundEffect TurnRight;

	private SoundEffect sRumble;

	private SoundEmitter eRumble;

	private float lastAngle;

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { get; set; }

	[ServiceDependency]
	public ITombstoneService TombstoneService { private get; set; }

	public TombstonesHost(Game game)
		: base(game)
	{
		base.DrawOrder = 6;
	}

	public override void Initialize()
	{
		base.Initialize();
		CameraManager.ViewpointChanged += OnViewpointChanged;
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void OnViewpointChanged()
	{
		foreach (TombstoneState trackedStone in TrackedStones)
		{
			trackedStone.TrySpin();
		}
	}

	private void TryInitialize()
	{
		TrackedStones.Clear();
		GrabSound = null;
		TurnLeft = null;
		TurnRight = null;
		sRumble = null;
		eRumble = null;
		foreach (ArtObjectInstance value2 in LevelManager.ArtObjects.Values)
		{
			if (value2.ArtObject.ActorType == ActorType.Tombstone)
			{
				TrackedStones.Add(new TombstoneState(this, value2));
			}
		}
		SkullAo = LevelManager.ArtObjects.Values.SingleOrDefault((ArtObjectInstance x) => x.ArtObjectName == "GIANT_SKULLAO");
		bool enabled = (base.Visible = TrackedStones.Count > 0 && SkullAo != null);
		base.Enabled = enabled;
		if (base.Enabled)
		{
			int value = SkullAo.ActorSettings.AttachedGroup.Value;
			SkullTopLayer = LevelManager.Groups[value].Triles.Where((TrileInstance x) => x.Trile.Faces[FaceOrientation.Back] == CollisionType.TopOnly).ToArray();
			SkullAttachedTriles = LevelManager.Groups[value].Triles.Where((TrileInstance x) => x.Trile.Immaterial).ToArray();
			SkullAttachedTrilesOriginalStates = SkullAttachedTriles.Select((TrileInstance x) => new Vector4(x.Position, x.Phi)).ToArray();
			GrabSound = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/GrabLever");
			TurnLeft = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Graveyard/TombRotateLeft");
			TurnRight = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Graveyard/TombRotateRight");
			sRumble = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/Rumble");
			eRumble = sRumble.Emit(loop: true, paused: true);
			int num = TrackedStones.Count((TombstoneState x) => x.LastViewpoint == TrackedStones[0].LastViewpoint);
			SkullRotates = num < 4;
			TombstoneService.UpdateAlignCount(num);
			OriginalRotation = SkullAo.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI / 2f);
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (EngineState.Loading || EngineState.InMap || EngineState.Paused || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		float num = float.MaxValue;
		TombstoneState tombstoneState = null;
		foreach (TombstoneState trackedStone in TrackedStones)
		{
			if (trackedStone.Update(gameTime.ElapsedGameTime))
			{
				float num2 = trackedStone.ArtObject.Position.Dot(CameraManager.Viewpoint.ForwardVector());
				if (num2 < num)
				{
					tombstoneState = trackedStone;
					num = num2;
				}
			}
		}
		tombstoneState?.GrabOnto();
		if (SkullRotates)
		{
			RotateSkull();
		}
	}

	private void RotateSkull()
	{
		InterpolatedRotation = Quaternion.Slerp(InterpolatedRotation, StopSkullRotations ? OriginalRotation : CameraManager.Rotation, 0.05f);
		if (InterpolatedRotation == CameraManager.Rotation)
		{
			if (eRumble.Cue.State != SoundState.Paused)
			{
				eRumble.Cue.Pause();
			}
			if (StopSkullRotations)
			{
				SkullRotates = false;
				StopSkullRotations = false;
			}
			return;
		}
		if (FezMath.AlmostEqual(InterpolatedRotation, CameraManager.Rotation) || FezMath.AlmostEqual(-InterpolatedRotation, CameraManager.Rotation))
		{
			InterpolatedRotation = CameraManager.Rotation;
		}
		SkullAo.Rotation = InterpolatedRotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, -(float)Math.PI / 2f);
		ToAxisAngle(ref InterpolatedRotation, out var axis, out var angle);
		float num = lastAngle - angle;
		if (Math.Abs(num) > 0.1f)
		{
			lastAngle = angle;
			return;
		}
		for (int i = 0; i < SkullAttachedTriles.Length; i++)
		{
			TrileInstance trileInstance = SkullAttachedTriles[i];
			Vector4 vector = SkullAttachedTrilesOriginalStates[i];
			trileInstance.Position = Vector3.Transform(vector.XYZ() + new Vector3(0.5f) - SkullAo.Position, InterpolatedRotation) + SkullAo.Position - new Vector3(0.5f);
			trileInstance.Phi = FezMath.WrapAngle(vector.W + (float)((!(axis.Y > 0f)) ? 1 : (-1)) * angle);
			LevelMaterializer.GetTrileMaterializer(trileInstance.Trile).UpdateInstance(trileInstance);
		}
		if (SkullTopLayer.Contains(PlayerManager.Ground.First))
		{
			Vector3 position = PlayerManager.Position;
			PlayerManager.Position = Vector3.Transform(PlayerManager.Position - SkullAo.Position, Quaternion.CreateFromAxisAngle(axis, num)) + SkullAo.Position;
			CameraManager.Center += PlayerManager.Position - position;
		}
		if ((double)Math.Abs(axis.Y) > 0.5)
		{
			float num2 = num * 5f;
			CameraManager.InterpolatedCenter += new Vector3(RandomHelper.Between(0f - num2, num2), RandomHelper.Between(0f - num2, num2), RandomHelper.Between(0f - num2, num2));
			if (eRumble.Cue.State == SoundState.Paused)
			{
				eRumble.Cue.Resume();
			}
			eRumble.VolumeFactor = FezMath.Saturate(Math.Abs(num2) * 25f);
		}
		if (InterpolatedRotation == CameraManager.Rotation)
		{
			RotateSkullTriles();
		}
		lastAngle = angle;
	}

	private void RotateSkullTriles()
	{
		TrileInstance[] skullAttachedTriles = SkullAttachedTriles;
		foreach (TrileInstance instance in skullAttachedTriles)
		{
			LevelManager.UpdateInstance(instance);
		}
	}

	private static void ToAxisAngle(ref Quaternion q, out Vector3 axis, out float angle)
	{
		angle = (float)Math.Acos(MathHelper.Clamp(q.W, -1f, 1f));
		float num = (float)Math.Sin(angle);
		float num2 = 1f / ((num == 0f) ? 1f : num);
		angle *= 2f;
		axis = new Vector3((0f - q.X) * num2, (0f - q.Y) * num2, (0f - q.Z) * num2);
	}

	public override void Draw(GameTime gameTime)
	{
		_ = EngineState.Loading;
	}
}
