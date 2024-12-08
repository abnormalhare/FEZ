using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class SuckedIn : PlayerAction
{
	private SoundEffect suckedSound;

	public SuckedIn(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		suckedSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/SuckedIn");
	}

	protected override void TestConditions()
	{
		if (base.PlayerManager.Action == ActionType.SuckedIn || base.PlayerManager.Action.IsEnteringDoor() || base.PlayerManager.Action == ActionType.OpeningTreasure || base.PlayerManager.Action == ActionType.FindingTreasure)
		{
			return;
		}
		foreach (Volume currentVolume in base.PlayerManager.CurrentVolumes)
		{
			if (currentVolume.ActorSettings != null && currentVolume.ActorSettings.IsBlackHole)
			{
				Vector3 vector = currentVolume.To - currentVolume.From;
				Vector3 vector2 = (currentVolume.From + currentVolume.To) / 2f - vector / 2f * base.CameraManager.Viewpoint.ForwardVector();
				base.PlayerManager.Action = ActionType.SuckedIn;
				base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + vector2 * base.CameraManager.Viewpoint.DepthMask() + -0.25f * base.CameraManager.Viewpoint.ForwardVector();
				currentVolume.ActorSettings.Sucking = true;
				break;
			}
		}
	}

	protected override void Begin()
	{
		base.Begin();
		base.PlayerManager.LookingDirection = base.PlayerManager.LookingDirection.GetOpposite();
		base.PlayerManager.CarriedInstance = null;
		base.PlayerManager.Action = ActionType.SuckedIn;
		base.PlayerManager.Ground = default(MultipleHits<TrileInstance>);
		suckedSound.EmitAt(base.PlayerManager.Position);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Respawn();
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.SuckedIn;
	}
}
