using System;
using FezEngine.Structure;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class ReadTurnAround : PlayerAction
{
	private SoundEffect sTurnAway;

	private SoundEffect sTurnBack;

	public ReadTurnAround(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sTurnAway = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TurnAway");
		sTurnBack = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TurnBack");
	}

	protected override void Begin()
	{
		base.Begin();
		sTurnAway.EmitAt(base.PlayerManager.Position);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.CanControl && base.PlayerManager.Action == ActionType.ReadTurnAround)
		{
			base.PlayerManager.Animation.Timing.Restart();
			base.PlayerManager.Action = ActionType.EndReadTurnAround;
			sTurnBack.EmitAt(base.PlayerManager.Position);
		}
		if (base.PlayerManager.Action == ActionType.EndReadTurnAround && base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.Idle;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.ReadTurnAround)
		{
			return type == ActionType.EndReadTurnAround;
		}
		return true;
	}
}
