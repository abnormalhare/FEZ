using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

public class Crush : PlayerAction
{
	private const float Duration = 1.75f;

	private float AccumlatedTime;

	private Vector3 crushPosition;

	public Crush(Game game)
		: base(game)
	{
	}

	protected override void Begin()
	{
		base.PlayerManager.Velocity = Vector3.Zero;
		crushPosition = base.PlayerManager.Position;
		AccumlatedTime = 0f;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		AccumlatedTime += (float)elapsed.TotalSeconds;
		base.PlayerManager.Position = crushPosition;
		base.PlayerManager.Animation.Timing.Update(elapsed, 2f);
		if (AccumlatedTime > 1.75f * ((base.PlayerManager.Action == ActionType.CrushHorizontal) ? 1.2f : 1f))
		{
			base.PlayerManager.Respawn();
		}
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.CrushVertical)
		{
			return type == ActionType.CrushHorizontal;
		}
		return true;
	}
}
