using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class ClimbOverLadder : PlayerAction
{
	private Vector3 camOrigin;

	private SoundEffect climbOverSound;

	private SoundEffect sLedgeLand;

	private int lastFrame;

	public ClimbOverLadder(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		climbOverSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/ClimbOverLadder");
		sLedgeLand = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeLand");
	}

	protected override void Begin()
	{
		base.PlayerManager.Velocity = Vector3.Zero;
		base.PlayerManager.Position += base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign() * 1f / 16f;
		base.PlayerManager.Position -= Vector3.UnitY * 0.5f / 16f;
		base.PlayerManager.Position *= 16f;
		base.PlayerManager.Position = base.PlayerManager.Position.Round();
		base.PlayerManager.Position /= 16f;
		base.PlayerManager.Position -= Vector3.UnitY * 0.5f / 16f;
		camOrigin = base.CameraManager.Center;
		climbOverSound.EmitAt(base.PlayerManager.Position);
		lastFrame = -1;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.HeldInstance.PhysicsState != null)
		{
			base.PlayerManager.Velocity = base.PlayerManager.HeldInstance.PhysicsState.Velocity;
			camOrigin += base.PlayerManager.HeldInstance.PhysicsState.Velocity;
		}
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign() * 10f / 16f;
		if (lastFrame != base.PlayerManager.Animation.Timing.Frame && base.PlayerManager.Animation.Timing.Frame == 5)
		{
			sLedgeLand.EmitAt(base.PlayerManager.Position);
		}
		lastFrame = base.PlayerManager.Animation.Timing.Frame;
		if (!base.CameraManager.StickyCam)
		{
			_ = base.CameraManager.ConstrainedCenter;
		}
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.HeldInstance = null;
			base.PlayerManager.Action = ActionType.Idle;
			base.PlayerManager.Position += vector;
			Vector3 position = base.PlayerManager.Position;
			base.PlayerManager.Position += 0.5f * Vector3.UnitY;
			base.PlayerManager.Velocity = Vector3.Down;
			base.PhysicsManager.Update(base.PlayerManager);
			if (!base.PlayerManager.Grounded)
			{
				base.PlayerManager.Velocity = Vector3.Zero;
				base.PlayerManager.Position = position;
			}
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.ClimbOverLadder;
	}
}
