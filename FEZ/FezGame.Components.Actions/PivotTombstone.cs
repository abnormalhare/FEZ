using System;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class PivotTombstone : PlayerAction
{
	private SoundEffect sTurnAway;

	private SoundEffect sTurnBack;

	protected override bool ViewTransitionIndependent => true;

	public PivotTombstone(Game game)
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
		if (base.PlayerManager.Action == ActionType.GrabTombstone && base.PlayerManager.Animation.Timing.Ended && !base.InputManager.GrabThrow.IsDown())
		{
			base.PlayerManager.Action = ActionType.LetGoOfTombstone;
			base.PlayerManager.Animation.Timing.Restart();
			sTurnBack.EmitAt(base.PlayerManager.Position);
		}
		if (base.PlayerManager.Action == ActionType.LetGoOfTombstone && base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.Idle;
		}
		base.PlayerManager.Background = false;
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.LetGoOfTombstone && type != ActionType.PivotTombstone)
		{
			return type == ActionType.GrabTombstone;
		}
		return true;
	}
}
