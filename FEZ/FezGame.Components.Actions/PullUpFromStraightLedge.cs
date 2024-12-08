using System;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class PullUpFromStraightLedge : PlayerAction
{
	private Vector3 camOrigin;

	private SoundEffect pullSound;

	private SoundEffect landSound;

	public PullUpFromStraightLedge(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		pullSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/StraightLedgeHoist");
		landSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeLand");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.ShimmyFront:
		case ActionType.ShimmyBack:
			if (((base.InputManager.Jump == FezButtonState.Pressed && !base.InputManager.Down.IsDown()) || base.InputManager.Up == FezButtonState.Pressed) && base.LevelManager.NearestTrile(base.PlayerManager.HeldInstance.Center).Deep != null)
			{
				base.PlayerManager.Action = (base.PlayerManager.Action.FacesBack() ? ActionType.PullUpBack : ActionType.PullUpFront);
			}
			break;
		case ActionType.GrabLedgeFront:
		case ActionType.GrabLedgeBack:
			if (((base.InputManager.Jump == FezButtonState.Pressed && !base.InputManager.Down.IsDown()) || base.InputManager.Up == FezButtonState.Pressed || (base.InputManager.Up == FezButtonState.Down && base.PlayerManager.Animation.Timing.Ended)) && base.LevelManager.NearestTrile(base.PlayerManager.HeldInstance.Center).Deep != null)
			{
				base.PlayerManager.Action = (base.PlayerManager.Action.FacesBack() ? ActionType.PullUpBack : ActionType.PullUpFront);
			}
			break;
		case ActionType.PullUpFront:
		case ActionType.PullUpBack:
		case ActionType.LowerToLedge:
			break;
		}
	}

	protected override void Begin()
	{
		pullSound.EmitAt(base.PlayerManager.Position);
		camOrigin = base.CameraManager.Center;
		base.PlayerManager.Velocity = Vector3.Zero;
		Waiters.Wait(0.5, delegate
		{
			landSound.EmitAt(base.PlayerManager.Position);
		});
		base.GomezService.OnHoist();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		Vector3 vector = base.PlayerManager.Size.Y / 2f * Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
		if (base.PlayerManager.HeldInstance.PhysicsState != null)
		{
			base.PlayerManager.Position += base.PlayerManager.HeldInstance.PhysicsState.Velocity;
			camOrigin += base.PlayerManager.HeldInstance.PhysicsState.Velocity;
		}
		if (!base.CameraManager.StickyCam && !base.CameraManager.Constrained)
		{
			base.CameraManager.Center = Vector3.Lerp(camOrigin, camOrigin + vector, base.PlayerManager.Animation.Timing.NormalizedStep);
		}
		base.PlayerManager.SplitUpCubeCollectorOffset = vector * base.PlayerManager.Animation.Timing.NormalizedStep;
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Position += vector;
			base.PlayerManager.SplitUpCubeCollectorOffset = Vector3.Zero;
			base.PlayerManager.Position += 0.5f * Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
			base.PlayerManager.Velocity -= Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
			base.PhysicsManager.Update(base.PlayerManager);
			base.PlayerManager.HeldInstance = null;
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.PullUpFront)
		{
			return type == ActionType.PullUpBack;
		}
		return true;
	}
}
