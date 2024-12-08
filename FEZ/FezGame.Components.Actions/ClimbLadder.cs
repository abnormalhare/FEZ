using System;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class ClimbLadder : PlayerAction
{
	private const float ClimbingSpeed = 0.425f;

	private ClimbingApproach currentApproach;

	private bool shouldSyncAnimationHalfway;

	private Vector3? lastGrabbedLocation;

	private SoundEffect climbSound;

	private int lastFrame;

	public ClimbLadder(Game game)
		: base(game)
	{
		base.UpdateOrder = 1;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		climbSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/ClimbLadder");
	}

	public override void Initialize()
	{
		base.CameraManager.ViewpointChanged += ChangeApproach;
		base.Initialize();
	}

	private void ChangeApproach()
	{
		lastGrabbedLocation = null;
		if (IsActionAllowed(base.PlayerManager.Action) && base.CameraManager.Viewpoint.IsOrthographic() && base.CameraManager.Viewpoint != base.CameraManager.LastViewpoint && !base.PlayerManager.IsOnRotato)
		{
			int distance = base.CameraManager.Viewpoint.GetDistance(base.CameraManager.LastViewpoint);
			int num = (int)(currentApproach + distance);
			if (num > 4)
			{
				num -= 4;
			}
			if (num < 1)
			{
				num += 4;
			}
			currentApproach = (ClimbingApproach)num;
			RefreshPlayerAction();
			RefreshPlayerDirection();
			shouldSyncAnimationHalfway = true;
		}
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.LookingLeft:
		case ActionType.LookingRight:
		case ActionType.LookingUp:
		case ActionType.LookingDown:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Jumping:
		case ActionType.Lifting:
		case ActionType.Falling:
		case ActionType.Bouncing:
		case ActionType.Flying:
		case ActionType.Dropping:
		case ActionType.Sliding:
		case ActionType.Landing:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
		{
			TrileInstance trileInstance = IsOnLadder(out currentApproach);
			if (currentApproach == ClimbingApproach.None)
			{
				break;
			}
			bool flag = false;
			if (base.InputManager.Down.IsDown() && base.PlayerManager.Grounded)
			{
				TrileInstance surface = base.LevelManager.NearestTrile(trileInstance.Center - Vector3.UnitY).Surface;
				flag = surface != null && surface.Trile.ActorSettings.Type == ActorType.Ladder;
			}
			FezButtonState fezButtonState = (base.PlayerManager.Grounded ? FezButtonState.Pressed : FezButtonState.Down);
			if (!flag && base.InputManager.Up != fezButtonState && (base.PlayerManager.Grounded || (currentApproach != ClimbingApproach.Left && currentApproach != ClimbingApproach.Right) || Math.Sign(base.InputManager.Movement.X) != currentApproach.AsDirection().Sign()))
			{
				break;
			}
			if (lastGrabbedLocation.HasValue)
			{
				Vector3 value = lastGrabbedLocation.Value;
				Vector3 a = base.PlayerManager.Position - value;
				Vector3 b = base.CameraManager.Viewpoint.SideMask();
				if (Math.Abs(a.Dot(b)) <= 1f && (double)a.Dot(Vector3.UnitY) < 1.625 && (double)a.Dot(Vector3.UnitY) > -0.5)
				{
					break;
				}
			}
			base.PlayerManager.HeldInstance = trileInstance;
			switch (currentApproach)
			{
			case ClimbingApproach.Back:
				base.PlayerManager.NextAction = ActionType.BackClimbingLadder;
				break;
			case ClimbingApproach.Right:
			case ClimbingApproach.Left:
				base.PlayerManager.NextAction = ActionType.SideClimbingLadder;
				break;
			case ClimbingApproach.Front:
				base.PlayerManager.NextAction = ActionType.FrontClimbingLadder;
				break;
			}
			if (base.PlayerManager.Grounded)
			{
				ActionType actionType = ((currentApproach == ClimbingApproach.Back) ? ActionType.IdleToClimb : ((currentApproach == ClimbingApproach.Front) ? ActionType.IdleToFrontClimb : ((currentApproach == ClimbingApproach.Back) ? ActionType.IdleToClimb : ActionType.IdleToSideClimb)));
				if (base.CollisionManager.CollidePoint(GetDestination(), Vector3.Down, QueryOptions.None, 0f, base.CameraManager.Viewpoint).Collided)
				{
					base.WalkTo.Destination = GetDestination;
					base.WalkTo.NextAction = actionType;
					base.PlayerManager.Action = ActionType.WalkingTo;
				}
				else
				{
					base.PlayerManager.Action = actionType;
					base.PlayerManager.Position -= 0.15f * Vector3.UnitY;
				}
			}
			else
			{
				base.PlayerManager.Action = ((currentApproach == ClimbingApproach.Back) ? ActionType.JumpToClimb : ActionType.JumpToSideClimb);
			}
			if (currentApproach == ClimbingApproach.Left || currentApproach == ClimbingApproach.Right)
			{
				base.PlayerManager.LookingDirection = currentApproach.AsDirection();
			}
			break;
		}
		}
	}

	protected override void Begin()
	{
		base.PlayerManager.Position = base.PlayerManager.Position * Vector3.UnitY + (base.PlayerManager.HeldInstance.Position + FezMath.HalfVector) * FezMath.XZMask;
		if (base.InputManager.Down.IsDown())
		{
			base.PlayerManager.Position -= 0.002f * Vector3.UnitY;
		}
		base.GomezService.OnClimbLadder();
	}

	private TrileInstance IsOnLadder(out ClimbingApproach approach)
	{
		Vector3 vector = base.CameraManager.Viewpoint.ForwardVector();
		Vector3 vector2 = base.CameraManager.Viewpoint.RightVector();
		float num = float.MaxValue;
		bool flag = false;
		TrileInstance trileInstance = null;
		bool flag2 = true;
		if (currentApproach == ClimbingApproach.None)
		{
			QueryOptions options = (base.PlayerManager.Background ? QueryOptions.Background : QueryOptions.None);
			NearestTriles nearestTriles = base.LevelManager.NearestTrile(base.PlayerManager.Center, options);
			flag2 = nearestTriles.Surface != null && nearestTriles.Surface.Trile.ActorSettings.Type == ActorType.Ladder;
		}
		foreach (NearestTriles value in base.PlayerManager.AxisCollision.Values)
		{
			if (value.Surface != null && TestLadderCollision(value.Surface, onAxis: true))
			{
				TrileInstance surface = value.Surface;
				float num2 = surface.Position.Dot(vector);
				if (flag2 && num2 < num)
				{
					num = num2;
					trileInstance = surface;
				}
			}
		}
		if (trileInstance == null)
		{
			foreach (NearestTriles value2 in base.PlayerManager.AxisCollision.Values)
			{
				if (value2.Surface != null && TestLadderCollision(value2.Surface, onAxis: false))
				{
					TrileInstance surface2 = value2.Surface;
					float num3 = surface2.Position.Dot(vector);
					if (flag2 && num3 < num)
					{
						flag = true;
						num = num3;
						trileInstance = surface2;
					}
				}
			}
		}
		if (trileInstance != null)
		{
			float num4 = FezMath.OrientationFromPhi(FezMath.WrapAngle(trileInstance.Trile.ActorSettings.Face.ToPhi() + trileInstance.Phi)).AsVector().Dot(flag ? vector2 : vector);
			if (flag)
			{
				approach = ((!(num4 > 0f)) ? ClimbingApproach.Right : ClimbingApproach.Left);
			}
			else
			{
				approach = ((num4 > 0f) ? ClimbingApproach.Front : ClimbingApproach.Back);
			}
		}
		else
		{
			approach = ClimbingApproach.None;
		}
		return trileInstance;
	}

	private bool TestLadderCollision(TrileInstance instance, bool onAxis)
	{
		TrileActorSettings actorSettings = instance.Trile.ActorSettings;
		Axis axis = FezMath.AxisFromPhi(FezMath.WrapAngle(actorSettings.Face.ToPhi() + instance.Phi));
		if (actorSettings.Type == ActorType.Ladder)
		{
			return axis == base.CameraManager.Viewpoint.VisibleAxis() == onAxis;
		}
		return false;
	}

	public override void Update(GameTime gameTime)
	{
		if (base.PlayerManager.Velocity.Y < -0.01f)
		{
			lastGrabbedLocation = null;
		}
		if (shouldSyncAnimationHalfway && (double)base.CameraManager.ViewTransitionStep >= 0.5)
		{
			shouldSyncAnimationHalfway = false;
			SyncAnimation(isActive: true);
		}
		base.Update(gameTime);
	}

	private Vector3 GetDestination()
	{
		return base.PlayerManager.Position * Vector3.UnitY + (base.PlayerManager.HeldInstance.Position + FezMath.HalfVector) * FezMath.XZMask;
	}

	private bool TestLadderTopLimit(ref Vector3 upDownMovement, Vector3 forward)
	{
		if (base.PlayerManager.Center.Y < base.LevelManager.Size.Y - 1f && base.PlayerManager.Center.Y > 1f)
		{
			QueryOptions options = (base.PlayerManager.Background ? QueryOptions.Background : QueryOptions.None);
			NearestTriles nearestTriles = base.LevelManager.NearestTrile(base.PlayerManager.Center + Vector3.Down, options);
			NearestTriles nearestTriles2 = base.LevelManager.NearestTrile(base.PlayerManager.Center + upDownMovement, options);
			NearestTriles nearestTriles3 = base.LevelManager.NearestTrile(base.PlayerManager.Center + upDownMovement + upDownMovement.Sign() * new Vector3(0f, 0.5f, 0f), options);
			bool flag = false;
			if ((nearestTriles2.Surface == null || (flag = nearestTriles3.Deep != null && base.PlayerManager.Position.Dot(forward) > nearestTriles3.Deep.Center.Dot(forward))) && (nearestTriles.Deep == null || nearestTriles.Deep.GetRotatedFace(base.PlayerManager.Background ? base.CameraManager.VisibleOrientation : base.CameraManager.VisibleOrientation.GetOpposite()) == CollisionType.None))
			{
				upDownMovement = Vector3.Zero;
				if (!flag && ((base.PlayerManager.LookingDirection == HorizontalDirection.Left && base.InputManager.Left.IsDown()) || (base.PlayerManager.LookingDirection == HorizontalDirection.Right && base.InputManager.Right.IsDown())))
				{
					if (nearestTriles2.Deep == null || nearestTriles.Surface == null)
					{
						return false;
					}
					float num = nearestTriles2.Deep.Center.Dot(forward);
					float num2 = nearestTriles.Surface.Center.Dot(forward);
					if (num > num2)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		ClimbingApproach approach;
		TrileInstance trileInstance = IsOnLadder(out approach);
		base.PlayerManager.HeldInstance = trileInstance;
		if (trileInstance == null || currentApproach == ClimbingApproach.None)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		lastGrabbedLocation = base.PlayerManager.Position;
		RefreshPlayerAction();
		RefreshPlayerDirection();
		base.PlayerManager.Position = base.PlayerManager.Position * Vector3.UnitY + (trileInstance.Position + FezMath.HalfVector) * FezMath.XZMask;
		Vector3 upDownMovement = base.InputManager.Movement.Y * 4.7f * 0.425f * (float)elapsed.TotalSeconds * Vector3.UnitY;
		Vector3 forward = base.CameraManager.Viewpoint.ForwardVector() * ((!base.PlayerManager.Background) ? 1 : (-1));
		if (!TestLadderTopLimit(ref upDownMovement, forward))
		{
			base.PlayerManager.Action = ActionType.ClimbOverLadder;
			return false;
		}
		float num = FezMath.Saturate(Math.Abs(base.PlayerManager.Animation.Timing.NormalizedStep * 2f % 1f - 0.5f)) * 1.4f + 0.25f;
		int frame = base.PlayerManager.Animation.Timing.Frame;
		if (lastFrame != frame)
		{
			if (frame == 1 || frame == 4)
			{
				climbSound.EmitAt(base.PlayerManager.Position);
			}
			lastFrame = frame;
		}
		base.PlayerManager.Velocity = upDownMovement * num;
		if (trileInstance.PhysicsState != null)
		{
			base.PlayerManager.Velocity += trileInstance.PhysicsState.Velocity;
		}
		int num2 = Math.Sign(upDownMovement.Y);
		base.PlayerManager.Animation.Timing.Update(elapsed, num2);
		base.PlayerManager.GroundedVelocity = base.PlayerManager.Velocity;
		MultipleHits<CollisionResult> multipleHits = base.CollisionManager.CollideEdge(base.PlayerManager.Center, upDownMovement, base.PlayerManager.Size / 2f, Direction2D.Vertical);
		if (upDownMovement.Y < 0f && (multipleHits.NearLow.Collided || multipleHits.FarHigh.Collided))
		{
			TrileInstance surface = base.LevelManager.NearestTrile(multipleHits.First.Destination.Center).Surface;
			if (surface != null && surface.Trile.ActorSettings.Type == ActorType.Ladder && currentApproach == ClimbingApproach.Back)
			{
				base.PlayerManager.Center += upDownMovement;
			}
			else
			{
				lastGrabbedLocation = null;
				base.PlayerManager.HeldInstance = null;
				base.PlayerManager.Action = ActionType.Falling;
			}
		}
		return false;
	}

	private void RefreshPlayerAction()
	{
		switch (currentApproach)
		{
		case ClimbingApproach.Back:
			base.PlayerManager.Action = ActionType.BackClimbingLadder;
			break;
		case ClimbingApproach.Front:
			base.PlayerManager.Action = ActionType.FrontClimbingLadder;
			break;
		case ClimbingApproach.Right:
		case ClimbingApproach.Left:
			base.PlayerManager.Action = ActionType.SideClimbingLadder;
			break;
		}
	}

	private void RefreshPlayerDirection()
	{
		if (base.PlayerManager.Action == ActionType.SideClimbingLadder)
		{
			base.PlayerManager.LookingDirection = currentApproach.AsDirection();
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return base.PlayerManager.Action.IsClimbingLadder();
	}
}
