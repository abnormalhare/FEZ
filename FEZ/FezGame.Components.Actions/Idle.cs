using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Idle : PlayerAction
{
	private TimeSpan changeAnimationIn;

	private ActionType lastSpecialIdle;

	private SoundEmitter lastSpecialIdleSound;

	private int lastFrame = -1;

	private SoundEffect sBlink;

	private SoundEffect sYawn;

	private SoundEffect sHatGrab;

	private SoundEffect sHatThrow;

	private SoundEffect sHatCatch;

	private SoundEffect sHatFinalThrow;

	private SoundEffect sHatFallOnHead;

	private SoundEffect sLayDown;

	private SoundEffect sSnore;

	private SoundEffect sWakeUp;

	private SoundEffect sIdleTurnLeft;

	private SoundEffect sIdleTurnRight;

	private SoundEffect sIdleTurnUp;

	private SoundEffect sIdleFaceFront;

	public Idle(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		base.CameraManager.ViewpointChanged += delegate
		{
			if (base.PlayerManager.Action == ActionType.Teetering)
			{
				base.PlayerManager.Action = ActionType.Idle;
			}
		};
		sBlink = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Blink");
		sYawn = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Yawn");
		sHatGrab = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/HatGrab");
		sHatThrow = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/HatThrow");
		sHatCatch = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/HatCatch");
		sHatFinalThrow = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/HatFinalThrow");
		sHatFallOnHead = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/HatFallOnHead");
		sLayDown = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LayDown");
		sSnore = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Snore");
		sWakeUp = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/WakeUp");
		sIdleTurnLeft = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/IdleTurnLeft");
		sIdleTurnRight = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/IdleTurnRight");
		sIdleTurnUp = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/IdleTurnUp");
		sIdleFaceFront = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/IdleFaceFront");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Dropping:
		case ActionType.Sliding:
		case ActionType.Grabbing:
		case ActionType.Pushing:
		{
			bool flag = base.CollisionManager.GravityFactor < 0f;
			if (FezMath.AlmostEqual(base.PlayerManager.Velocity.XZ(), Vector2.Zero) && base.InputManager.Movement.X == 0f && base.PlayerManager.PushedInstance == null && (flag ? (base.PlayerManager.Velocity.Y >= 0f) : (base.PlayerManager.Velocity.Y <= 0f)))
			{
				base.PlayerManager.Action = ActionType.Idle;
			}
			break;
		}
		}
	}

	protected override void Begin()
	{
		lastFrame = -1;
		ScheduleSpecialIdle();
	}

	private void ScheduleSpecialIdle()
	{
		changeAnimationIn = TimeSpan.FromSeconds(RandomHelper.Between(7.0, 9.0));
	}

	protected override void End()
	{
		base.End();
		if (lastSpecialIdleSound != null && !lastSpecialIdleSound.Dead)
		{
			lastSpecialIdleSound.FadeOutAndDie(0.1f);
			lastSpecialIdleSound = null;
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		int num = base.PlayerManager.Animation.Timing.Frame;
		switch (base.PlayerManager.Action)
		{
		case ActionType.IdleLookAround:
			if (lastFrame != num)
			{
				if (num == 1)
				{
					lastSpecialIdleSound = sIdleTurnLeft.EmitAt(base.PlayerManager.Position);
				}
				if (num == 7)
				{
					lastSpecialIdleSound = sIdleTurnRight.EmitAt(base.PlayerManager.Position);
				}
				if (num == 13)
				{
					lastSpecialIdleSound = sIdleTurnUp.EmitAt(base.PlayerManager.Position);
				}
				if (num == 19)
				{
					lastSpecialIdleSound = sIdleFaceFront.EmitAt(base.PlayerManager.Position);
				}
			}
			if (CheckNextIdle())
			{
				num = -1;
			}
			break;
		case ActionType.IdlePlay:
			if (lastFrame != num)
			{
				if (num == 2)
				{
					lastSpecialIdleSound = sHatGrab.EmitAt(base.PlayerManager.Position);
				}
				if (num == 6 || num == 13 || num == 20)
				{
					lastSpecialIdleSound = sHatThrow.EmitAt(base.PlayerManager.Position);
				}
				if (num == 10 || num == 17 || num == 24)
				{
					lastSpecialIdleSound = sHatCatch.EmitAt(base.PlayerManager.Position);
				}
				if (num == 27)
				{
					lastSpecialIdleSound = sHatFinalThrow.EmitAt(base.PlayerManager.Position);
				}
				if (num == 31)
				{
					lastSpecialIdleSound = sHatFallOnHead.EmitAt(base.PlayerManager.Position);
				}
			}
			if (CheckNextIdle())
			{
				num = -1;
			}
			break;
		case ActionType.IdleSleep:
			if (lastFrame != num)
			{
				if (num == 1)
				{
					lastSpecialIdleSound = sYawn.EmitAt(base.PlayerManager.Position);
				}
				if (num == 3)
				{
					lastSpecialIdleSound = sLayDown.EmitAt(base.PlayerManager.Position);
				}
				if (num == 11 || num == 21 || num == 31 || num == 41)
				{
					lastSpecialIdleSound = sSnore.EmitAt(base.PlayerManager.Position);
				}
				if (num == 50)
				{
					lastSpecialIdleSound = sWakeUp.EmitAt(base.PlayerManager.Position);
				}
				if (num == 51)
				{
					sBlink.EmitAt(base.PlayerManager.Position);
				}
			}
			if (CheckNextIdle())
			{
				num = -1;
			}
			break;
		case ActionType.IdleYawn:
			if (lastFrame != num && num == 0)
			{
				lastSpecialIdleSound = sYawn.EmitAt(base.PlayerManager.Position);
			}
			if (CheckNextIdle())
			{
				num = -1;
			}
			break;
		default:
			if (base.PlayerManager.CanControl)
			{
				changeAnimationIn -= elapsed;
			}
			if (!base.GameState.TimePaused && !base.PlayerManager.Hidden && !base.GameState.FarawaySettings.InTransition && !base.PlayerManager.InDoorTransition && lastFrame != num && (num == 1 || num == 13))
			{
				sBlink.EmitAt(base.PlayerManager.Position);
			}
			if (changeAnimationIn.Ticks > 0)
			{
				break;
			}
			switch (lastSpecialIdle)
			{
			case ActionType.IdleYawn:
				base.PlayerManager.Action = ActionType.IdleSleep;
				break;
			case ActionType.IdleSleep:
				base.PlayerManager.Action = ActionType.IdleLookAround;
				break;
			case ActionType.IdleLookAround:
				if (base.PlayerManager.HideFez)
				{
					base.PlayerManager.Action = ActionType.IdleYawn;
				}
				else
				{
					base.PlayerManager.Action = ActionType.IdlePlay;
				}
				break;
			case ActionType.None:
			case ActionType.IdlePlay:
				base.PlayerManager.Action = ActionType.IdleYawn;
				break;
			}
			lastSpecialIdle = base.PlayerManager.Action;
			num = -1;
			break;
		}
		lastFrame = num;
		return true;
	}

	private bool CheckNextIdle()
	{
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			ScheduleSpecialIdle();
			base.PlayerManager.Action = ActionType.Idle;
			return true;
		}
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.Idle && type != ActionType.IdleSleep && type != ActionType.IdlePlay && type != ActionType.IdleLookAround)
		{
			return type == ActionType.IdleYawn;
		}
		return true;
	}
}
