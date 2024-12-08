using System;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Land : PlayerAction
{
	private SoundEffect landSound;

	public Land(Game game)
		: base(game)
	{
	}

	protected override void TestConditions()
	{
		ActionType action = base.PlayerManager.Action;
		if (action == ActionType.Falling && base.PlayerManager.Grounded)
		{
			base.PlayerManager.Action = ActionType.Landing;
		}
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		landSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Land");
	}

	protected override void Begin()
	{
		base.Begin();
		base.InputManager.ActiveGamepad.Vibrate(VibrationMotor.RightHigh, 0.4000000059604645, TimeSpan.FromSeconds(0.15000000596046448));
		landSound.EmitAt(base.PlayerManager.Position);
		base.GomezService.OnLand();
		base.GameState.JetpackMode = false;
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
		return type == ActionType.Landing;
	}
}
