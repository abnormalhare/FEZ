using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

public class Die : PlayerAction
{
	public Die(Game game)
		: base(game)
	{
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.CameraManager.Constrained = false;
			base.PlayerManager.Respawn();
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Dying;
	}
}
