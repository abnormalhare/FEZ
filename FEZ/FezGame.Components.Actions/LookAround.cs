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

public class LookAround : PlayerAction
{
	private ActionType nextAction;

	private SoundEffect rightSound;

	private SoundEffect leftSound;

	private SoundEffect upSound;

	private SoundEffect downSound;

	[ServiceDependency]
	public IMouseStateManager MouseState { private get; set; }

	public LookAround(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		rightSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LookRight");
		leftSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LookLeft");
		upSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LookUp");
		downSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LookDown");
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
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
			if (base.PlayerManager.CanControl)
			{
				Vector2 vector = base.InputManager.FreeLook;
				if (MouseState.LeftButton.State == MouseButtonStates.Dragging)
				{
					vector = -vector;
				}
				if ((double)vector.Y < -0.4)
				{
					nextAction = ActionType.LookingDown;
				}
				else if ((double)vector.Y > 0.4)
				{
					nextAction = ActionType.LookingUp;
				}
				else if ((double)vector.X < -0.4)
				{
					nextAction = ActionType.LookingLeft;
				}
				else if ((double)vector.X > 0.4)
				{
					nextAction = ActionType.LookingRight;
				}
				else if (FezMath.AlmostEqual(base.InputManager.FreeLook, Vector2.Zero))
				{
					nextAction = ActionType.Idle;
				}
			}
			else
			{
				nextAction = ((!base.PlayerManager.Action.IsLookingAround()) ? ActionType.Idle : base.PlayerManager.Action);
			}
			if (base.PlayerManager.LookingDirection == HorizontalDirection.Left && (nextAction == ActionType.LookingLeft || nextAction == ActionType.LookingRight))
			{
				nextAction = ((nextAction == ActionType.LookingRight) ? ActionType.LookingLeft : ActionType.LookingRight);
			}
			if (base.PlayerManager.Action.IsIdle() && nextAction != 0 && nextAction != ActionType.Idle)
			{
				PlaySound();
				base.PlayerManager.Action = nextAction;
				nextAction = ActionType.None;
			}
			if (nextAction == base.PlayerManager.Action)
			{
				nextAction = ActionType.None;
			}
			break;
		default:
			nextAction = ActionType.None;
			break;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		if (base.PlayerManager.CanControl)
		{
			base.GomezService.OnLookAround();
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if ((double)base.PlayerManager.Animation.Timing.NormalizedStep <= 0.55)
		{
			base.PlayerManager.Animation.Timing.Update(elapsed);
		}
		else if (nextAction != 0)
		{
			base.PlayerManager.Animation.Timing.Update(elapsed, 1.25f);
		}
		if (base.PlayerManager.Animation.Timing.Ended && nextAction != 0)
		{
			PlaySound();
			base.PlayerManager.Action = nextAction;
			nextAction = ActionType.None;
		}
		return false;
	}

	private void PlaySound()
	{
		switch (nextAction)
		{
		case ActionType.LookingRight:
			rightSound.EmitAt(base.PlayerManager.Position);
			break;
		case ActionType.LookingLeft:
			leftSound.EmitAt(base.PlayerManager.Position);
			break;
		case ActionType.LookingUp:
			upSound.EmitAt(base.PlayerManager.Position);
			break;
		case ActionType.LookingDown:
			downSound.EmitAt(base.PlayerManager.Position);
			break;
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.LookingDown && type != ActionType.LookingLeft && type != ActionType.LookingRight)
		{
			return type == ActionType.LookingUp;
		}
		return true;
	}
}
