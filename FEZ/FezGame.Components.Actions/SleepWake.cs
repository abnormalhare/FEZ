using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

internal class SleepWake : PlayerAction
{
	public SleepWake(Game game)
		: base(game)
	{
	}

	protected override void Begin()
	{
		base.PlayerManager.Animation.Timing.Frame = 8;
		base.Begin();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.Idle;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.SleepWake;
	}
}
