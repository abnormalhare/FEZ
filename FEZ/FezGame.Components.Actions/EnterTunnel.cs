using System;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class EnterTunnel : PlayerAction
{
	private SoundEffect SwooshRight;

	private Vector3 originalForward;

	private Vector3 originalPosition;

	private float distanceToCover;

	protected override bool ViewTransitionIndependent => true;

	public EnterTunnel(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		SwooshRight = base.CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateCenter");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.CarryIdle:
		case ActionType.CarryWalk:
		case ActionType.CarrySlide:
		case ActionType.CarryHeavyIdle:
		case ActionType.CarryHeavyWalk:
		case ActionType.CarryHeavySlide:
		case ActionType.Sliding:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
			if (base.PlayerManager.TunnelVolume.HasValue && !base.PlayerManager.Background && base.InputManager.ExactUp == FezButtonState.Pressed)
			{
				if (base.PlayerManager.CarriedInstance == null)
				{
					base.WalkTo.Destination = GetDestination;
					base.PlayerManager.Action = ActionType.WalkingTo;
					base.WalkTo.NextAction = ActionType.EnteringTunnel;
				}
				else
				{
					Vector3 position = base.PlayerManager.Position;
					base.PlayerManager.Position = GetDestination();
					base.PlayerManager.CarriedInstance.Position += base.PlayerManager.Position - position;
					base.PlayerManager.Action = (base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsHeavy() ? ActionType.EnterTunnelCarryHeavy : ActionType.EnterTunnelCarry);
				}
			}
			break;
		}
	}

	private Vector3 GetDestination()
	{
		if (!base.PlayerManager.TunnelVolume.HasValue)
		{
			return base.PlayerManager.Position;
		}
		Volume volume = base.LevelManager.Volumes[base.PlayerManager.TunnelVolume.Value];
		Vector3 vector = (volume.From + volume.To) / 2f;
		return base.PlayerManager.Position * (Vector3.UnitY + base.CameraManager.Viewpoint.DepthMask()) + vector * base.CameraManager.Viewpoint.SideMask();
	}

	protected override void Begin()
	{
		SwooshRight.Emit();
		originalForward = base.CameraManager.Viewpoint.ForwardVector();
		base.CameraManager.ChangeViewpoint(base.CameraManager.Viewpoint.GetRotatedView(2));
		base.PlayerManager.LookingDirection = HorizontalDirection.Right;
		base.PlayerManager.Velocity = Vector3.Zero;
		originalPosition = base.PlayerManager.Position;
		distanceToCover = (base.LevelManager.NearestTrile(base.PlayerManager.Ground.First.Center, QueryOptions.None, base.CameraManager.Viewpoint).Deep.Center - originalPosition).Dot(originalForward);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.CameraManager.ViewTransitionStep != 0f)
		{
			Vector3 position = base.PlayerManager.Position;
			base.PlayerManager.Position = originalPosition + Vector3.Lerp(Vector3.Zero, originalForward * distanceToCover, base.CameraManager.ViewTransitionStep);
			if (base.PlayerManager.CarriedInstance != null)
			{
				base.PlayerManager.CarriedInstance.Position += base.PlayerManager.Position - position;
			}
		}
		if (base.CameraManager.ActionRunning)
		{
			base.PlayerManager.Action = ((base.PlayerManager.Action == ActionType.EnteringTunnel) ? ActionType.Idle : ((base.PlayerManager.Action == ActionType.EnterTunnelCarry) ? ActionType.CarryIdle : ActionType.CarryHeavyIdle));
			base.PlayerManager.Background = false;
			return false;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (base.PlayerManager.Action != ActionType.EnteringTunnel && base.PlayerManager.Action != ActionType.EnterTunnelCarry)
		{
			return base.PlayerManager.Action == ActionType.EnterTunnelCarryHeavy;
		}
		return true;
	}
}
