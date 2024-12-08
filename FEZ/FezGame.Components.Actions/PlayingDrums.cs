using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

internal class PlayingDrums : PlayerAction
{
	public PlayingDrums(Game game)
		: base(game)
	{
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Action == ActionType.DrumsIdle)
		{
			return true;
		}
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type.IsPlayingDrums();
	}
}
