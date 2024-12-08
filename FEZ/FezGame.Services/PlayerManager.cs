using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public class PlayerManager : IPlayerManager, IComplexPhysicsEntity, IPhysicsEntity
{
	private const float TrixelSize = 0.0625f;

	private readonly Vector3 BaseSize = new Vector3(0.625f, 0.9375f, 1f);

	private Vector3 position;

	private Vector3 variableSize;

	private ActionType action;

	private TrileInstance lastHeldInstance;

	private MultipleHits<TrileInstance> lastGround;

	private Dictionary<ActionType, AnimatedTexture> HatAnimations;

	private Dictionary<ActionType, AnimatedTexture> NoHatAnimations;

	private readonly Stack<object> controlStack = new Stack<object>();

	private TrileInstance carriedInstance;

	private TrileInstance lastCarriedInstance;

	public Vector3 Velocity { get; set; }

	public Vector3? GroundedVelocity { get; set; }

	public MultipleHits<TrileInstance> Ground { get; set; }

	public MultipleHits<CollisionResult> Ceiling { get; set; }

	public bool MustBeClampedToGround { get; set; }

	public bool CanRotate { get; set; }

	public List<Volume> CurrentVolumes { get; private set; }

	public TrileInstance HeldInstance { get; set; }

	public TrileInstance PushedInstance { get; set; }

	public TimeSpan AirTime { get; set; }

	public bool CanDoubleJump { get; set; }

	public float BlinkSpeed { get; set; }

	public bool NoVelocityClamping => false;

	public HorizontalDirection MovingDirection { get; set; }

	public HorizontalDirection LookingDirection { get; set; }

	public Vector3 GroundMovement { get; set; }

	public ActionType NextAction { get; set; }

	public ActionType LastAction { get; set; }

	public ActionType LastGroundedAction { get; private set; }

	public Viewpoint LastGroundedView { get; private set; }

	public HorizontalDirection LastGroundedLookingDirection { get; private set; }

	public TrileInstance CheckpointGround { get; set; }

	public Vector3 RespawnPosition { get; set; }

	public Vector3 LeaveGroundPosition { get; set; }

	public float OffsetAtLeaveGround { get; set; }

	public bool InDoorTransition { get; set; }

	public PointCollision[] CornerCollision { get; private set; }

	public Dictionary<VerticalDirection, NearestTriles> AxisCollision { get; private set; }

	public bool CanControl
	{
		get
		{
			return controlStack.Count == 0;
		}
		set
		{
			if (value && controlStack.Count > 0)
			{
				controlStack.Pop();
			}
			else if (!value)
			{
				controlStack.Push(new object());
			}
		}
	}

	public bool Background { get; set; }

	public float Elasticity => 0f;

	public GomezHost MeshHost { get; set; }

	public AnimatedTexture Animation { get; set; }

	public TimeSpan InvincibilityLeft { get; set; }

	public string NextLevel { get; set; }

	public int? DoorVolume { get; set; }

	public int? TunnelVolume { get; set; }

	public bool SpinThroughDoor { get; set; }

	public int? PipeVolume { get; set; }

	public bool IgnoreFreefall { get; set; }

	public bool IsOnRotato { get; set; }

	public bool DoorEndsTrial { get; set; }

	public TrileInstance ForcedTreasure { get; set; }

	public Vector3 SplitUpCubeCollectorOffset { get; set; }

	public bool HideFez
	{
		get
		{
			return GameState.SaveData.FezHidden;
		}
		set
		{
			GameState.SaveData.FezHidden = value;
		}
	}

	public float GomezOpacity { get; set; }

	public WarpPanel WarpPanel { get; set; }

	public Viewpoint OriginWarpViewpoint { get; set; }

	public bool FreshlyRespawned { get; set; }

	public bool Swimming => Action.IsSwimming();

	public Vector3 Size
	{
		get
		{
			if (CameraManager.Viewpoint == Viewpoint.Left || CameraManager.Viewpoint == Viewpoint.Right)
			{
				return variableSize.ZYX();
			}
			return variableSize;
		}
	}

	public Vector3 Center
	{
		get
		{
			return position + CameraManager.Viewpoint.RightVector() * LookingDirection.Sign() * 0.125f + Vector3.UnitY * (variableSize.Y - BaseSize.Y);
		}
		set
		{
			Position = value - CameraManager.Viewpoint.RightVector() * LookingDirection.Sign() * 0.125f - Vector3.UnitY * (variableSize.Y - BaseSize.Y);
		}
	}

	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
		}
	}

	public bool Grounded => Ground.First != null;

	public MultipleHits<CollisionResult> WallCollision { get; set; }

	public ActionType Action
	{
		get
		{
			return action;
		}
		set
		{
			if (action != value)
			{
				LastAction = action;
			}
			action = value;
		}
	}

	public TrileInstance CarriedInstance
	{
		get
		{
			return carriedInstance;
		}
		set
		{
			carriedInstance = value;
			if (Action != ActionType.ThrowingHeavy)
			{
				SyncCollisionSize();
			}
			if (carriedInstance != null)
			{
				CameraManager.RecordNewCarriedInstancePhi();
				carriedInstance.PhysicsState.Ground = default(MultipleHits<TrileInstance>);
			}
			else if (Action != ActionType.WakingUp)
			{
				ForceOverlapsDetermination();
			}
		}
	}

	public bool Climbing
	{
		get
		{
			if (!Action.IsClimbingLadder())
			{
				return Action.IsClimbingVine();
			}
			return true;
		}
	}

	public bool Sliding => Action == ActionType.Sliding;

	public bool HandlesZClamping => Action.HandlesZClamping();

	public bool Hidden { get; set; }

	public bool FullBright { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public IVolumeService VolumeService { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public PlayerManager()
	{
		HatAnimations = new Dictionary<ActionType, AnimatedTexture>(Util.GetValues<ActionType>().Count() - 1, ActionTypeComparer.Default);
		NoHatAnimations = new Dictionary<ActionType, AnimatedTexture>(ActionTypeComparer.Default);
		Reset();
	}

	public void FillAnimations()
	{
		HatAnimations.Clear();
		NoHatAnimations.Clear();
		foreach (ActionType value in Util.GetValues<ActionType>())
		{
			if (value != 0)
			{
				string text = "Character Animations/Gomez/" + value.GetAnimationPath();
				HatAnimations.Add(value, CMProvider.Global.Load<AnimatedTexture>(text));
				if (MemoryContentManager.AssetExists(text.Replace('/', '\\') + "_NoHat"))
				{
					AnimatedTexture animatedTexture = CMProvider.Global.Load<AnimatedTexture>(text + "_NoHat");
					animatedTexture.NoHat = true;
					NoHatAnimations.Add(value, animatedTexture);
				}
			}
		}
	}

	public AnimatedTexture GetAnimation(ActionType type)
	{
		if (HideFez && !GameState.SaveData.IsNewGamePlus && NoHatAnimations.ContainsKey(type))
		{
			return NoHatAnimations[type];
		}
		return HatAnimations[type];
	}

	public void Reset()
	{
		Ground = default(MultipleHits<TrileInstance>);
		CornerCollision = new PointCollision[4];
		AxisCollision = new Dictionary<VerticalDirection, NearestTriles>(VerticalDirectionComparer.Default)
		{
			{
				VerticalDirection.Up,
				default(NearestTriles)
			},
			{
				VerticalDirection.Down,
				default(NearestTriles)
			}
		};
		Background = false;
		variableSize = BaseSize;
		CanRotate = true;
		controlStack.Clear();
		CanControl = true;
		Action = ActionType.Idle;
		CurrentVolumes = new List<Volume>();
		GomezOpacity = 1f;
		InDoorTransition = false;
		FullBright = false;
	}

	public void CopyTo(IPlayerManager other)
	{
		other.AxisCollision[VerticalDirection.Up] = ((IComplexPhysicsEntity)this).AxisCollision[VerticalDirection.Up];
		other.AxisCollision[VerticalDirection.Down] = ((IComplexPhysicsEntity)this).AxisCollision[VerticalDirection.Down];
		other.Background = ((IPhysicsEntity)this).Background;
		other.Ceiling = ((IComplexPhysicsEntity)this).Ceiling;
		other.Center = ((IPhysicsEntity)this).Center;
		other.Position = Position;
		other.Action = Action;
		other.Ground = ((IPhysicsEntity)this).Ground;
		other.GroundedVelocity = ((IComplexPhysicsEntity)this).GroundedVelocity;
		other.GroundMovement = ((IPhysicsEntity)this).GroundMovement;
		other.MovingDirection = ((IComplexPhysicsEntity)this).MovingDirection;
		other.MustBeClampedToGround = ((IComplexPhysicsEntity)this).MustBeClampedToGround;
		other.Velocity = ((IPhysicsEntity)this).Velocity;
		other.WallCollision = ((IPhysicsEntity)this).WallCollision;
		other.CarriedInstance = CarriedInstance;
		other.LookingDirection = LookingDirection;
		if (other.LastAction == ActionType.ThrowingHeavy && other.Action != ActionType.ThrowingHeavy)
		{
			other.SyncCollisionSize();
		}
	}

	public void SyncCollisionSize()
	{
		if (carriedInstance != null && lastCarriedInstance == null)
		{
			if (carriedInstance.Trile.ActorSettings.Type.IsLight())
			{
				variableSize = new Vector3(0.75f, 1.9375f, 0.75f);
			}
			else
			{
				variableSize = new Vector3(0.75f, 1.75f, 0.75f);
			}
			Position -= (variableSize - BaseSize) / 2f * Vector3.UnitY;
		}
		else if (carriedInstance == null && lastCarriedInstance != null)
		{
			Position += (variableSize - BaseSize) / 2f * Vector3.UnitY;
			variableSize = BaseSize;
		}
		lastCarriedInstance = carriedInstance;
	}

	public void RespawnAtCheckpoint()
	{
		HeldInstance = null;
		CarriedInstance = null;
		IsOnRotato = false;
		GameState.SkipRendering = true;
		CameraManager.ChangeViewpoint(GameState.SaveData.View, 0f);
		GameState.SkipRendering = false;
		CameraManager.SnapInterpolation();
		TrileInstance trileInstance = CheckpointGround ?? LevelManager.ActualInstanceAt(GameState.SaveData.Ground) ?? LevelManager.NearestTrile(GameState.SaveData.Ground).Deep;
		if (trileInstance == null)
		{
			Vector3 respawnPosition = (Position = GameState.SaveData.Ground + Size / 2f * Vector3.UnitY * Math.Sign(CollisionManager.GravityFactor));
			RespawnPosition = respawnPosition;
		}
		else
		{
			Vector3 respawnPosition = (Position = trileInstance.Center + (trileInstance.TransformedSize / 2f + Size / 2f) * Vector3.UnitY * Math.Sign(CollisionManager.GravityFactor));
			RespawnPosition = respawnPosition;
		}
		if (GameState.FarawaySettings.InTransition)
		{
			CameraManager.SnapInterpolation();
		}
		Action = ActionType.WakingUp;
		LookingDirection = HorizontalDirection.Right;
		ForceOverlapsDetermination();
		PhysicsManager.HugWalls(this, determineBackground: false, postRotation: false, keepInFront: true);
	}

	public void Respawn()
	{
		FreshlyRespawned = true;
		foreach (Volume currentVolume in CurrentVolumes)
		{
			if (!currentVolume.PlayerInside)
			{
				VolumeService.OnExit(currentVolume.Id);
			}
			currentVolume.PlayerInside = false;
		}
		CurrentVolumes.Clear();
		IsOnRotato = false;
		Position = RespawnPosition + Vector3.UnitY * 1f / 32f * Math.Sign(CollisionManager.GravityFactor);
		if (LastGroundedAction == ActionType.None)
		{
			LastGroundedAction = ActionType.Idle;
		}
		Action = LastGroundedAction;
		LookingDirection = LastGroundedLookingDirection;
		Velocity = Vector3.Down * 1f / 16f * Math.Sign(CollisionManager.GravityFactor);
		HeldInstance = lastHeldInstance;
		Ground = lastGround;
		if (LastGroundedView == Viewpoint.None)
		{
			LastGroundedView = CameraManager.Viewpoint;
		}
		CameraManager.ChangeViewpoint(LastGroundedView);
		GroundedVelocity = null;
		Array.Clear(CornerCollision, 0, 4);
		if (!CameraManager.Constrained)
		{
			float num = 4f * (float)((!LevelManager.Descending) ? 1 : (-1)) / CameraManager.PixelsPerTrixel;
			CameraManager.Center = Position + Vector3.UnitY * num;
		}
		LevelManager.ScreenInvalidated += ForceOverlapsDetermination;
	}

	public void RecordRespawnInformation()
	{
		RecordRespawnInformation(markCheckpoint: false);
	}

	public void RecordRespawnInformation(bool markCheckpoint)
	{
		if (Grounded || Climbing || action == ActionType.GrabCornerLedge || action.IsSwimming() || action == ActionType.EnteringPipe)
		{
			TrileInstance first = Ground.First;
			if (Climbing)
			{
				Vector3 vector = CameraManager.Viewpoint.SideMask();
				LeaveGroundPosition = vector * Position.Floor() + vector * 0.5f + Vector3.UnitY * ((float)(int)Math.Ceiling(Position.Y) + 0.5f) + (Vector3.One - vector - Vector3.UnitY) * Position;
			}
			else if (action == ActionType.GrabCornerLedge || action.IsSwimming() || action == ActionType.EnteringPipe)
			{
				LeaveGroundPosition = Position;
			}
			else
			{
				LeaveGroundPosition = first.Center + (first.TransformedSize.Y / 2f + Size.Y / 2f) * Vector3.UnitY * Math.Sign(CollisionManager.GravityFactor);
			}
			OffsetAtLeaveGround = CameraManager.ViewOffset.Y;
			if (!Action.DisallowsRespawn() && CarriedInstance == null && !Background && (!Grounded || (first.PhysicsState == null && !first.Unsafe)) && (HeldInstance == null || HeldInstance.PhysicsState == null) && ((Grounded && first.Trile.ActorSettings.Type.IsSafe()) || (Action == ActionType.GrabCornerLedge && HeldInstance != null && !HeldInstance.Unsafe && HeldInstance.Trile.ActorSettings.Type.IsSafe())))
			{
				LastGroundedAction = Action;
				LastGroundedView = CameraManager.Viewpoint;
				LastGroundedLookingDirection = LookingDirection;
				RespawnPosition = LeaveGroundPosition;
				lastGround = Ground;
				lastHeldInstance = HeldInstance;
			}
			if (markCheckpoint || LastGroundedView == Viewpoint.None)
			{
				GameState.SaveData.View = CameraManager.Viewpoint;
				GameState.SaveData.TimeOfDay = TimeManager.CurrentTime.TimeOfDay;
				CheckpointGround = first;
				GameState.SaveData.Ground = CheckpointGround.Center;
				GameState.SaveData.Level = LevelManager.Name;
			}
		}
	}

	public void ForceOverlapsDetermination()
	{
		PhysicsManager.DetermineOverlaps(this);
	}
}
