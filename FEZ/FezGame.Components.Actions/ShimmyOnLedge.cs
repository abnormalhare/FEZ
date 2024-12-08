using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class ShimmyOnLedge : PlayerAction
{
	private const float ShimmyingSpeed = 0.15f;

	private SoundEffect shimmySound;

	private int lastFrame;

	public ShimmyOnLedge(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		shimmySound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeShimmy");
	}

	protected override void TestConditions()
	{
		ActionType action = base.PlayerManager.Action;
		if ((action == ActionType.GrabLedgeFront || action == ActionType.GrabLedgeBack) && base.InputManager.Movement.X != 0f && base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = (base.PlayerManager.Action.FacesBack() ? ActionType.ShimmyBack : ActionType.ShimmyFront);
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		float num = FezMath.Saturate(Math.Abs(3f / (float)base.PlayerManager.Animation.Timing.EndFrame - base.PlayerManager.Animation.Timing.Step)) * 2f;
		int frame = base.PlayerManager.Animation.Timing.Frame;
		if (lastFrame != frame)
		{
			if (frame == 2)
			{
				shimmySound.EmitAt(base.PlayerManager.Position);
			}
			lastFrame = frame;
		}
		TrileInstance heldInstance = base.PlayerManager.HeldInstance;
		if (!base.PlayerManager.IsOnRotato)
		{
			base.PlayerManager.HeldInstance = base.PlayerManager.AxisCollision[VerticalDirection.Down].Deep;
		}
		bool num2 = base.PlayerManager.HeldInstance == null;
		Vector3 vector = base.CameraManager.Viewpoint.ForwardVector() * ((!base.PlayerManager.Background) ? 1 : (-1));
		bool flag = base.PlayerManager.HeldInstance != null && base.PlayerManager.HeldInstance.GetRotatedFace(base.CameraManager.Viewpoint.VisibleOrientation()) == CollisionType.None;
		if (num2 || (flag && base.PlayerManager.HeldInstance.Position.Dot(vector) > heldInstance.Position.Dot(vector)))
		{
			base.PlayerManager.Action = (base.PlayerManager.Action.FacesBack() ? ActionType.ToCornerBack : ActionType.ToCornerFront);
			base.PlayerManager.HeldInstance = heldInstance;
			base.PlayerManager.LookingDirection = base.PlayerManager.LookingDirection.GetOpposite();
			return false;
		}
		if (flag)
		{
			base.PlayerManager.Action = ActionType.Dropping;
			base.PlayerManager.HeldInstance = null;
			return false;
		}
		float num3 = base.InputManager.Movement.X * 4.7f * 0.15f * (float)elapsed.TotalSeconds;
		if (base.PlayerManager.Action != ActionType.ShimmyBack && base.PlayerManager.Action != ActionType.ShimmyFront)
		{
			num3 *= 0.6f;
		}
		base.PlayerManager.Velocity = num3 * base.CameraManager.Viewpoint.RightVector() * (1f + num);
		if (base.InputManager.Movement.X == 0f)
		{
			base.PlayerManager.Action = (base.PlayerManager.Action.FacesBack() ? ActionType.GrabLedgeBack : ActionType.GrabLedgeFront);
		}
		else
		{
			base.PlayerManager.GroundedVelocity = base.PlayerManager.Velocity;
		}
		if (base.InputManager.RotateLeft == FezButtonState.Pressed || base.InputManager.RotateRight == FezButtonState.Pressed)
		{
			base.PlayerManager.Action = (base.PlayerManager.Action.FacesBack() ? ActionType.GrabLedgeBack : ActionType.GrabLedgeFront);
		}
		if (base.PlayerManager.Action == ActionType.ShimmyBack || base.PlayerManager.Action == ActionType.ShimmyFront)
		{
			base.PlayerManager.Animation.Timing.Update(elapsed, Math.Abs(base.InputManager.Movement.X));
			if (base.PlayerManager.HeldInstance.PhysicsState != null)
			{
				base.PlayerManager.Position += base.PlayerManager.HeldInstance.PhysicsState.Velocity;
			}
		}
		Vector3 vector2 = base.CameraManager.Viewpoint.DepthMask();
		base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + base.PlayerManager.HeldInstance.Center * vector2 + vector * -(base.PlayerManager.HeldInstance.TransformedSize / 2f + base.PlayerManager.Size.X * vector2 / 4f);
		base.PhysicsManager.HugWalls(base.PlayerManager, determineBackground: false, postRotation: false, keepInFront: true);
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		switch (type)
		{
		case ActionType.GrabLedgeFront:
		case ActionType.GrabLedgeBack:
			return base.InputManager.Movement.X != 0f;
		default:
			return false;
		case ActionType.ShimmyFront:
		case ActionType.ShimmyBack:
			return true;
		}
	}
}
