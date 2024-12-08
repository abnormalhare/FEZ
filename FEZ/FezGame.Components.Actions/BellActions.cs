using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

internal class BellActions : PlayerAction
{
	protected override bool ViewTransitionIndependent => true;

	public BellActions(Game game)
		: base(game)
	{
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Action == ActionType.TurnToBell && base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.HitBell;
			base.PlayerManager.Animation.Timing.Restart();
		}
		if (base.PlayerManager.Action == ActionType.HitBell && base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.TurnAwayFromBell;
			base.PlayerManager.Animation.Timing.Restart();
		}
		if (base.PlayerManager.Action == ActionType.TurnAwayFromBell && base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.Idle;
			base.PlayerManager.Animation.Timing.Restart();
		}
		base.PlayerManager.Background = false;
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.TurnAwayFromBell && type != ActionType.HitBell)
		{
			return type == ActionType.TurnToBell;
		}
		return true;
	}
}
