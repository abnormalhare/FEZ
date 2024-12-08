using System;
using FezEngine.Structure;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class ExitDoor : PlayerAction
{
	private SoundEffect sound;

	public ExitDoor(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		sound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/ExitDoor");
	}

	protected override void Begin()
	{
		base.Begin();
		sound.EmitAt(base.PlayerManager.Position);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.ExitDoor && type != ActionType.ExitDoorCarry)
		{
			return type == ActionType.ExitDoorCarryHeavy;
		}
		return true;
	}
}
