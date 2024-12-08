using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

internal class Floating : PlayerAction
{
	public Floating(Game game)
		: base(game)
	{
	}

	protected override bool Act(TimeSpan elapsed)
	{
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Floating;
	}
}
