using System;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Bounce : PlayerAction
{
	private static readonly TimeSpan BounceVibrateTime = TimeSpan.FromSeconds(0.30000001192092896);

	private const float BouncerResponse = 0.32f;

	private SoundEffect bounceHigh;

	private SoundEffect bounceLow;

	public Bounce(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		bounceHigh = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/BounceHigh");
		bounceLow = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/BounceLow");
	}

	protected override void TestConditions()
	{
		if (base.PlayerManager.Action == ActionType.Bouncing && (base.GameState.InCutscene || !base.PlayerManager.CanControl))
		{
			base.PlayerManager.Action = ActionType.Landing;
			return;
		}
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Jumping:
		case ActionType.Falling:
		case ActionType.Dropping:
		case ActionType.Sliding:
		case ActionType.Landing:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
			if (base.PlayerManager.Grounded && base.PlayerManager.Ground.First.Trile.ActorSettings.Type == ActorType.Bouncer)
			{
				base.PlayerManager.Action = ActionType.Bouncing;
			}
			break;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		base.InputManager.ActiveGamepad.Vibrate(VibrationMotor.LeftLow, 0.5, BounceVibrateTime, EasingType.Quadratic);
		base.InputManager.ActiveGamepad.Vibrate(VibrationMotor.RightHigh, 0.6000000238418579, BounceVibrateTime, EasingType.Quadratic);
		if (RandomHelper.Probability(0.5))
		{
			bounceHigh.EmitAt(base.PlayerManager.Position);
		}
		else
		{
			bounceLow.EmitAt(base.PlayerManager.Position);
		}
		base.PlayerManager.Velocity *= new Vector3(1f, 0f, 1f);
		base.PlayerManager.Velocity += Vector3.UnitY * 0.32f;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Bouncing;
	}
}
