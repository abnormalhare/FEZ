using System;
using FezEngine.Structure;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class Standing : PlayerAction
{
	private SoundEffect sBlink;

	private int lastFrame;

	public Standing(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		sBlink = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Blink");
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Action == ActionType.StandWinking)
		{
			int frame = base.PlayerManager.Animation.Timing.Frame;
			if (lastFrame != frame && (frame == 1 || frame == 13))
			{
				sBlink.EmitAt(base.PlayerManager.Position);
			}
			lastFrame = frame;
		}
		return base.PlayerManager.Action == ActionType.StandWinking;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.Standing)
		{
			return type == ActionType.StandWinking;
		}
		return true;
	}
}
