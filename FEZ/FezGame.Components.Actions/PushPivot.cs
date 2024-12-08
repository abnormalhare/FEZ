using System;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class PushPivot : PlayerAction
{
	private SoundEffect sTurnAway;

	private SoundEffect sTurnBack;

	private SoundEffect sFallOnFace;

	private SoundEmitter eTurnAway;

	private SoundEmitter eTurnBack;

	private TimeSpan sinceStarted;

	private bool reverse;

	private int lastFrame;

	public PushPivot(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sTurnAway = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TurnAway");
		sTurnBack = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TurnBack");
		sFallOnFace = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Land");
	}

	protected override void Begin()
	{
		sinceStarted = TimeSpan.Zero;
		eTurnAway = sTurnAway.EmitAt(base.PlayerManager.Position);
		reverse = false;
		lastFrame = -1;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		int frame = base.PlayerManager.Animation.Timing.Frame;
		sinceStarted += elapsed;
		if (sinceStarted.TotalSeconds < 0.25 && !base.InputManager.GrabThrow.IsDown())
		{
			eTurnAway.FadeOutAndDie(0.1f);
			reverse = true;
		}
		if (reverse)
		{
			base.PlayerManager.Animation.Timing.Update(elapsed, -1f);
			if (base.PlayerManager.Animation.Timing.Step <= 0f)
			{
				base.PlayerManager.Action = ActionType.Idle;
			}
			return false;
		}
		if (base.PlayerManager.Animation.Timing.Frame == 32 && (eTurnBack == null || eTurnBack.Dead))
		{
			eTurnBack = sTurnBack.EmitAt(base.PlayerManager.Position);
		}
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		if (frame != lastFrame && frame == 18)
		{
			sFallOnFace.EmitAt(base.PlayerManager.Position);
		}
		lastFrame = frame;
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.PushingPivot;
	}
}
