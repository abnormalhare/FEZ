using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class FromCornerTransition : PlayerAction
{
	private SoundEffect transitionSound;

	public FromCornerTransition(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		transitionSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeToCorner");
	}

	protected override void Begin()
	{
		base.PlayerManager.Velocity = Vector3.Zero;
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		Vector3 vector2 = base.CameraManager.Viewpoint.ForwardVector();
		Vector3 vector3 = base.CameraManager.Viewpoint.DepthMask();
		Vector3 vector4 = (-vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f;
		base.PlayerManager.Position = base.PlayerManager.HeldInstance.Center + vector4 + vector2 * -(base.PlayerManager.HeldInstance.TransformedSize / 2f + base.PlayerManager.Size.X * vector3 / 4f);
		base.PlayerManager.ForceOverlapsDetermination();
		base.PhysicsManager.HugWalls(base.PlayerManager, determineBackground: false, postRotation: false, keepInFront: true);
		transitionSound.EmitAt(base.PlayerManager.Position);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.HeldInstance.PhysicsState != null)
		{
			base.PlayerManager.Position += base.PlayerManager.HeldInstance.PhysicsState.Velocity;
		}
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.GrabLedgeBack;
			Vector3 vector = base.CameraManager.Viewpoint.SideMask();
			Vector3 vector2 = base.CameraManager.Viewpoint.DepthMask();
			Vector3 vector3 = base.CameraManager.Viewpoint.ForwardVector();
			TrileInstance heldInstance = base.PlayerManager.HeldInstance;
			base.PlayerManager.Position = base.PlayerManager.Position * vector + heldInstance.Center * (Vector3.UnitY + vector2) + vector3 * (0f - (0.5f + base.PlayerManager.Size.X / 4f)) + heldInstance.TransformedSize * Vector3.UnitY / 2f * Math.Sign(base.CollisionManager.GravityFactor);
			base.PlayerManager.ForceOverlapsDetermination();
			base.PhysicsManager.HugWalls(base.PlayerManager, determineBackground: false, postRotation: false, keepInFront: true);
			base.PlayerManager.HeldInstance = base.PlayerManager.AxisCollision[VerticalDirection.Down].Deep;
			return false;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.FromCornerBack;
	}
}
