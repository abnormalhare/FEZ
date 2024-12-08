using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class ToCornerTransition : PlayerAction
{
	private SoundEffect transitionSound;

	public ToCornerTransition(Game game)
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
		transitionSound.EmitAt(base.PlayerManager.Position);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		Vector3 vector2 = base.CameraManager.Viewpoint.ForwardVector();
		Vector3 vector3 = base.CameraManager.Viewpoint.DepthMask();
		Vector3 vector4 = (-vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f;
		base.PlayerManager.Position = base.PlayerManager.HeldInstance.Center + vector4 + vector2 * -(base.PlayerManager.HeldInstance.TransformedSize / 2f + base.PlayerManager.Size.X * vector3 / 4f) * ((!base.PlayerManager.Background) ? 1 : (-1));
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = ActionType.GrabCornerLedge;
			base.PlayerManager.Background = false;
			return false;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.ToCornerFront)
		{
			return type == ActionType.ToCornerBack;
		}
		return true;
	}
}
