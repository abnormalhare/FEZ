using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class WalkRun : PlayerAction
{
	public const float SecondsBeforeRun = 0.2f;

	public const float RunAcceleration = 1.25f;

	public static readonly MovementHelper MovementHelper = new MovementHelper(4.7f, 5.875f, 0.2f);

	private int initialMovement;

	private SoundEffect turnAroundSound;

	public WalkRun(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		MovementHelper.Entity = base.PlayerManager;
		turnAroundSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TurnAround");
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
		case ActionType.Sliding:
		case ActionType.Grabbing:
		case ActionType.Pushing:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
			if (base.PlayerManager.Action != ActionType.Sliding || base.PlayerManager.LastAction != ActionType.Running || !TestForTurn())
			{
				if (base.PlayerManager.Grounded && base.InputManager.Movement.X != 0f && base.PlayerManager.PushedInstance == null)
				{
					base.PlayerManager.Action = ActionType.Walking;
				}
				else
				{
					MovementHelper.Reset();
				}
			}
			break;
		case ActionType.Walking:
		case ActionType.Running:
			TestForTurn();
			break;
		}
	}

	private bool TestForTurn()
	{
		int num = Math.Sign(base.InputManager.Movement.X);
		if (num != 0 && num != base.PlayerManager.LookingDirection.Sign())
		{
			initialMovement = num;
			base.PlayerManager.Action = ActionType.RunTurnAround;
			turnAroundSound.EmitAt(base.PlayerManager.Position);
			return true;
		}
		return false;
	}

	protected override void Begin()
	{
		MovementHelper.Reset();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Action == ActionType.RunTurnAround)
		{
			if (Math.Sign(base.InputManager.Movement.X) != initialMovement)
			{
				base.PlayerManager.LookingDirection = base.PlayerManager.LookingDirection.GetOpposite();
				base.PlayerManager.Action = ActionType.Idle;
				return false;
			}
			if (base.PlayerManager.Animation.Timing.Ended)
			{
				base.PlayerManager.LookingDirection = base.PlayerManager.LookingDirection.GetOpposite();
				base.PlayerManager.Action = ActionType.Running;
				return false;
			}
			base.PlayerManager.Animation.Timing.Update(elapsed, (1f + Math.Abs(base.CollisionManager.GravityFactor)) / 2f);
		}
		else if (base.PlayerManager.Action != ActionType.Landing)
		{
			float num2;
			if (MovementHelper.Running)
			{
				bool num = base.PlayerManager.Action == ActionType.Walking;
				base.PlayerManager.Action = ActionType.Running;
				SyncAnimation(isActive: true);
				if (num)
				{
					base.PlayerManager.Animation.Timing.Frame = 1;
				}
				num2 = 1.25f;
			}
			else
			{
				base.PlayerManager.Action = ActionType.Walking;
				num2 = Easing.EaseOut(Math.Min(1f, Math.Abs(base.InputManager.Movement.X) * 2f), EasingType.Cubic);
			}
			base.PlayerManager.Animation.Timing.Update(elapsed, num2 * (1f + Math.Abs(base.CollisionManager.GravityFactor)) / 2f);
		}
		MovementHelper.Update((float)elapsed.TotalSeconds);
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.Running && type != ActionType.Landing && type != ActionType.Walking)
		{
			return type == ActionType.RunTurnAround;
		}
		return true;
	}
}
