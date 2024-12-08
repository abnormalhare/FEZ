using System;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

public class Slide : PlayerAction
{
	public Slide(Game game)
		: base(game)
	{
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
			if (!FezMath.AlmostEqual(base.PlayerManager.Velocity.XZ(), Vector2.Zero) && FezMath.AlmostEqual(base.InputManager.Movement, Vector2.Zero))
			{
				base.PlayerManager.Action = ActionType.Sliding;
			}
			break;
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Sliding;
	}
}
