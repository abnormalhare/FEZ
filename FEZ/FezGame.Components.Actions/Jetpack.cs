using System;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

public class Jetpack : PlayerAction
{
	private const float JetpackSpeed = 0.075f;

	public Jetpack(Game game)
		: base(game)
	{
	}

	protected override void TestConditions()
	{
		if ((base.GameState.JetpackMode || base.GameState.DebugMode) && base.InputManager.Jump == FezButtonState.Down && base.PlayerManager.Action != ActionType.FindingTreasure && base.PlayerManager.Action != ActionType.Dying && base.PlayerManager.Action != ActionType.OpeningTreasure && base.PlayerManager.Action != ActionType.Suffering && base.LevelManager.Name != "VILLAGEVILLE_2D" && base.LevelManager.Name != "ELDERS" && base.LevelManager.Name != "VILLAGEVILLE_3D_END_64" && base.PlayerManager.Action != ActionType.LesserWarp && base.PlayerManager.Action != ActionType.GateWarp)
		{
			base.PlayerManager.CarriedInstance = null;
			base.PlayerManager.Action = ActionType.Flying;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		IPlayerManager playerManager = base.PlayerManager;
		TrileInstance carriedInstance = (base.PlayerManager.PushedInstance = null);
		playerManager.CarriedInstance = carriedInstance;
		base.CameraManager.Constrained = false;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.InputManager.Jump == FezButtonState.Down)
		{
			base.PlayerManager.Velocity += 0.15f * (float)Math.Sign(base.CollisionManager.GravityFactor) * 1.025f * Vector3.UnitY * 0.075f;
			base.PlayerManager.LeaveGroundPosition = new Vector3(base.PlayerManager.LeaveGroundPosition.X, base.PlayerManager.Position.Y, base.PlayerManager.LeaveGroundPosition.Z);
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Flying;
	}
}
