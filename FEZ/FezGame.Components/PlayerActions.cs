using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components.Actions;
using FezGame.Components.Scripting;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class PlayerActions : GameComponent
{
	private class ActionsManager : GameComponent
	{
		private readonly PlayerActions Host;

		[ServiceDependency]
		public IGameCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IInputManager InputManager { private get; set; }

		public ActionsManager(Game game, PlayerActions host)
			: base(game)
		{
			Host = host;
		}

		public override void Update(GameTime gameTime)
		{
			if (GameState.Paused || GameState.Loading || GameState.InCutscene || GameState.InMap || GameState.InFpsMode || GameState.InMenuCube)
			{
				return;
			}
			if (!PlayerManager.CanControl)
			{
				InputManager.SaveState();
				InputManager.Reset();
			}
			bool actionNotRunning = !CameraManager.ActionRunning;
			foreach (PlayerAction lightAction in Host.LightActions)
			{
				lightAction.LightUpdate(gameTime, actionNotRunning);
			}
			if (!PlayerManager.CanControl)
			{
				InputManager.RecoverState();
			}
		}
	}

	private HorizontalDirection oldLookDir;

	private int lastFrame;

	private readonly Dictionary<SurfaceType, SoundEffect[]> SurfaceHits = new Dictionary<SurfaceType, SoundEffect[]>(SurfaceTypeComparer.Default);

	private SoundEffect LeftStep;

	private SoundEffect RightStep;

	private bool isLeft;

	public const float PlayerSpeed = 4.7f;

	private readonly List<PlayerAction> LightActions = new List<PlayerAction>();

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	internal IScriptingManager ScriptingManager { get; set; }

	public PlayerActions(Game game)
		: base(game)
	{
		base.UpdateOrder = 1;
		WalkTo walkTo = new WalkTo(game);
		ServiceHelper.InjectServices(walkTo);
		ServiceHelper.AddComponent(walkTo, addServices: true);
		if (walkTo.UpdateOrder == 0)
		{
			LightActions.Add(walkTo);
			walkTo.Enabled = false;
		}
		AddAction(new Fall(game));
		AddAction(new Idle(game));
		AddAction(new DropDown(game));
		AddAction(new LowerToStraightLedge(game));
		AddAction(new Land(game));
		AddAction(new Slide(game));
		AddAction(new Lift(game));
		AddAction(new Jump(game));
		AddAction(new WalkRun(game));
		AddAction(new Bounce(game));
		AddAction(new ClimbLadder(game));
		AddAction(new ReadSign(game));
		AddAction(new FreeFall(game));
		AddAction(new Die(game));
		AddAction(new Victory(game));
		AddAction(new GrabCornerLedge(game));
		AddAction(new PullUpFromCornerLedge(game));
		AddAction(new LowerToCornerLedge(game));
		AddAction(new Carry(game));
		AddAction(new Throw(game));
		AddAction(new DropTrile(game));
		AddAction(new Suffer(game));
		AddAction(new EnterDoor(game));
		AddAction(new Grab(game));
		AddAction(new Push(game));
		AddAction(new SuckedIn(game));
		AddAction(new ClimbVine(game));
		AddAction(new WakingUp(game));
		AddAction(new Jetpack(game));
		AddAction(new OpenTreasure(game));
		AddAction(new OpenDoor(game));
		AddAction(new Swim(game));
		AddAction(new Sink(game));
		AddAction(new LookAround(game));
		AddAction(new Teeter(game));
		AddAction(new EnterTunnel(game));
		AddAction(new PushPivot(game));
		AddAction(new PullUpFromStraightLedge(game));
		AddAction(new GrabStraightLedge(game));
		AddAction(new ShimmyOnLedge(game));
		AddAction(new ToCornerTransition(game));
		AddAction(new FromCornerTransition(game));
		AddAction(new ToClimbTransition(game));
		AddAction(new ClimbOverLadder(game));
		AddAction(new PivotTombstone(game));
		AddAction(new EnterPipe(game));
		AddAction(new ExitDoor(game));
		AddAction(new LesserWarp(game));
		AddAction(new GateWarp(game));
		AddAction(new SleepWake(game));
		AddAction(new ReadTurnAround(game));
		AddAction(new BellActions(game));
		AddAction(new Crush(game));
		AddAction(new PlayingDrums(game));
		AddAction(new Floating(game));
		AddAction(new Standing(game));
		ServiceHelper.AddComponent(new ActionsManager(game, this));
	}

	private void AddAction(PlayerAction action)
	{
		ServiceHelper.AddComponent(action);
		if (action.UpdateOrder == 0 && !action.IsUpdateOverridden && action.GetType().Name != "Jump")
		{
			LightActions.Add(action);
			action.Enabled = false;
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		CameraManager.ViewpointChanged += delegate
		{
			LevelManager.ScreenInvalidated += delegate
			{
				PhysicsManager.DetermineOverlaps(PlayerManager);
				if (CameraManager.Viewpoint.IsOrthographic() && CameraManager.LastViewpoint != CameraManager.Viewpoint && !PlayerManager.HandlesZClamping)
				{
					CorrectWallOverlap(overcompensate: true);
					if (PhysicsManager.DetermineInBackground(PlayerManager, !PlayerManager.IsOnRotato, viewpointChanged: true, !PlayerManager.Climbing && !LevelManager.LowPass) && !CameraManager.Constrained)
					{
						_ = 4f * (float)((!LevelManager.Descending) ? 1 : (-1)) / CameraManager.PixelsPerTrixel;
						Vector3 vector = CameraManager.LastViewpoint.ScreenSpaceMask();
						CameraManager.Center = vector * CameraManager.Center + PlayerManager.Position * (Vector3.One - vector);
					}
				}
				PhysicsManager.DetermineOverlaps(PlayerManager);
			};
		};
		LevelManager.LevelChanged += delegate
		{
			LevelManager.ScreenInvalidated += delegate
			{
				PhysicsManager.HugWalls(PlayerManager, determineBackground: false, postRotation: false, !PlayerManager.Climbing);
			};
			if (string.IsNullOrEmpty(LevelManager.Name))
			{
				foreach (PlayerAction lightAction in LightActions)
				{
					lightAction.Reset();
				}
			}
		};
		foreach (SurfaceType value in Util.GetValues<SurfaceType>())
		{
			SurfaceHits.Add(value, (from f in CMProvider.GetAllIn("Sounds/Gomez\\Footsteps\\" + value)
				select CMProvider.Global.Load<SoundEffect>(f)).ToArray());
		}
		LeftStep = CMProvider.Global.Load<SoundEffect>("Sounds/Gomez\\Footsteps\\Left");
		RightStep = CMProvider.Global.Load<SoundEffect>("Sounds/Gomez\\Footsteps\\Right");
		ScriptingManager.CutsceneSkipped += delegate
		{
			while (!PlayerManager.CanControl)
			{
				PlayerManager.CanControl = true;
			}
		};
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || PlayerManager.Hidden || GameState.InCutscene)
		{
			return;
		}
		PlayerManager.FreshlyRespawned = false;
		_ = PlayerManager.Position;
		if (!PlayerManager.CanControl)
		{
			InputManager.SaveState();
			InputManager.Reset();
		}
		if (CameraManager.Viewpoint != Viewpoint.Perspective && CameraManager.ActionRunning && !GameState.InMenuCube && !GameState.Paused && CameraManager.RequestedViewpoint == Viewpoint.None && !GameState.InMap && !LevelManager.IsInvalidatingScreen)
		{
			if (PlayerManager.Action.AllowsLookingDirectionChange() && !FezMath.AlmostEqual(InputManager.Movement.X, 0f))
			{
				oldLookDir = PlayerManager.LookingDirection;
				PlayerManager.LookingDirection = FezMath.DirectionFromMovement(InputManager.Movement.X);
			}
			Vector3 velocity = PlayerManager.Velocity;
			PhysicsManager.Update(PlayerManager);
			if (PlayerManager.Grounded && PlayerManager.Ground.NearLow == null)
			{
				TrileInstance farHigh = PlayerManager.Ground.FarHigh;
				Vector3 vector = CameraManager.Viewpoint.RightVector() * PlayerManager.LookingDirection.Sign();
				Vector3 vector2 = farHigh.Center - farHigh.TransformedSize / 2f * vector;
				Vector3 vector3 = PlayerManager.Center + PlayerManager.Size / 2f * vector;
				float num = (vector2 - vector3).Dot(vector);
				if (num > -0.25f)
				{
					PlayerManager.Position -= Vector3.UnitY * 0.01f * Math.Sign(CollisionManager.GravityFactor);
					if (farHigh.GetRotatedFace(CameraManager.Viewpoint.VisibleOrientation()) == CollisionType.AllSides)
					{
						PlayerManager.Position += num * vector;
						PlayerManager.Velocity = velocity * Vector3.UnitY;
					}
					else
					{
						PlayerManager.Velocity = velocity;
					}
					PlayerManager.GroundedVelocity = PlayerManager.Velocity;
					PlayerManager.Ground = default(MultipleHits<TrileInstance>);
				}
			}
			PlayerManager.RecordRespawnInformation();
			if (!PlayerManager.Action.HandlesZClamping() && (oldLookDir != PlayerManager.LookingDirection || PlayerManager.LastAction == ActionType.RunTurnAround) && PlayerManager.Action != ActionType.Dropping && PlayerManager.Action != ActionType.GrabCornerLedge && PlayerManager.Action != ActionType.SuckedIn && PlayerManager.Action != ActionType.CrushVertical && PlayerManager.Action != ActionType.CrushHorizontal)
			{
				CorrectWallOverlap(overcompensate: false);
			}
		}
		if (PlayerManager.Grounded)
		{
			PlayerManager.IgnoreFreefall = false;
		}
		if (PlayerManager.Animation != null && lastFrame != PlayerManager.Animation.Timing.Frame)
		{
			if (PlayerManager.Grounded)
			{
				SurfaceType surfaceType = PlayerManager.Ground.First.Trile.SurfaceType;
				if (PlayerManager.Action == ActionType.Landing && PlayerManager.Animation.Timing.Frame == 0)
				{
					PlaySurfaceHit(surfaceType, withStep: false);
				}
				else if ((PlayerManager.Action == ActionType.PullUpBack || PlayerManager.Action == ActionType.PullUpFront || PlayerManager.Action == ActionType.PullUpCornerLedge) && PlayerManager.Animation.Timing.Frame == 5)
				{
					PlaySurfaceHit(surfaceType, withStep: false);
				}
				else if (PlayerManager.Action.GetAnimationPath() == "Walk")
				{
					if (PlayerManager.Animation.Timing.Frame == 1 || PlayerManager.Animation.Timing.Frame == 4)
					{
						if (PlayerManager.Action != ActionType.Sliding)
						{
							(isLeft ? LeftStep : RightStep).EmitAt(PlayerManager.Position, RandomHelper.Between(-0.10000000149011612, 0.10000000149011612), RandomHelper.Between(0.8999999761581421, 1.0));
							isLeft = !isLeft;
						}
						PlaySurfaceHit(surfaceType, withStep: false);
					}
				}
				else if (PlayerManager.Action == ActionType.Running)
				{
					if (PlayerManager.Animation.Timing.Frame == 0 || PlayerManager.Animation.Timing.Frame == 3)
					{
						PlaySurfaceHit(surfaceType, withStep: true);
					}
				}
				else if (PlayerManager.CarriedInstance != null)
				{
					if (PlayerManager.Action.GetAnimationPath() == "CarryHeavyWalk")
					{
						if (PlayerManager.Animation.Timing.Frame == 0 || PlayerManager.Animation.Timing.Frame == 4)
						{
							PlaySurfaceHit(surfaceType, withStep: true);
						}
					}
					else if (PlayerManager.Action.GetAnimationPath() == "CarryWalk" && (PlayerManager.Animation.Timing.Frame == 3 || PlayerManager.Animation.Timing.Frame == 7))
					{
						PlaySurfaceHit(surfaceType, withStep: true);
					}
				}
				else
				{
					isLeft = false;
				}
			}
			else
			{
				isLeft = false;
			}
			lastFrame = PlayerManager.Animation.Timing.Frame;
		}
		if (!PlayerManager.CanControl)
		{
			InputManager.RecoverState();
		}
	}

	private void PlaySurfaceHit(SurfaceType surfaceType, bool withStep)
	{
		if (withStep)
		{
			(isLeft ? LeftStep : RightStep).EmitAt(PlayerManager.Position, RandomHelper.Between(-0.10000000149011612, 0.10000000149011612), RandomHelper.Between(0.8999999761581421, 1.0));
			isLeft = !isLeft;
		}
		RandomHelper.InList(SurfaceHits[surfaceType]).EmitAt(PlayerManager.Position, RandomHelper.Between(-0.10000000149011612, 0.10000000149011612), RandomHelper.Between(0.8999999761581421, 1.0));
	}

	private void CorrectWallOverlap(bool overcompensate)
	{
		Vector3 vector = Vector3.Zero;
		float num = 0f;
		PointCollision[] cornerCollision = PlayerManager.CornerCollision;
		for (int i = 0; i < cornerCollision.Length; i++)
		{
			PointCollision pointCollision = cornerCollision[i];
			TrileInstance deep = pointCollision.Instances.Deep;
			if (deep != null && deep != PlayerManager.CarriedInstance && deep.GetRotatedFace(CameraManager.VisibleOrientation) == CollisionType.AllSides)
			{
				Vector3 vector2 = CameraManager.Viewpoint.SideMask();
				Vector3 vector3 = (deep.Center - PlayerManager.Center).Sign();
				Vector3 vector4 = deep.Center - vector3 * deep.TransformedSize / 2f;
				Vector3 point = pointCollision.Point;
				Vector3 vector5 = (vector4 - point) * vector2;
				if (vector5.Abs().Dot(vector2) > num)
				{
					num = vector5.Abs().Dot(vector2);
					vector = vector5;
				}
			}
		}
		if (num > 0f)
		{
			Vector3 vector6 = vector + vector.Sign() * 0.001f * 2f;
			PlayerManager.Position += vector6;
			if (PlayerManager.CarriedInstance != null && !CameraManager.ViewTransitionReached)
			{
				PlayerManager.CarriedInstance.Position += vector6;
			}
			if (PlayerManager.Velocity.Sign() == -vector.Sign())
			{
				Vector3 vector7 = vector.Sign().Abs();
				PlayerManager.Position -= PlayerManager.Velocity * vector7;
				PlayerManager.Velocity *= Vector3.One - vector7;
			}
		}
		if (LevelManager.Name == "BOILEROOM" && LevelManager.Volumes.TryGetValue(3, out var value))
		{
			BoundingBox boundingBox = value.BoundingBox;
			boundingBox.Max -= FezMath.XZMask * 1.5f;
			boundingBox.Min += FezMath.XZMask * 1.5f;
			if (boundingBox.Contains(PlayerManager.Position) == ContainmentType.Disjoint)
			{
				Vector3 position = PlayerManager.Position;
				position.X = MathHelper.Clamp(position.X, boundingBox.Min.X, boundingBox.Max.X);
				position.Z = MathHelper.Clamp(position.Z, boundingBox.Min.Z, boundingBox.Max.Z);
				PlayerManager.Position = position;
			}
		}
	}
}
