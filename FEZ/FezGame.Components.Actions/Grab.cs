using System;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Grab : PlayerAction
{
	private int pushingDirectionSign;

	private SoundEffect grabSound;

	private int lastFrame;

	public Grab(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		grabSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/GrabPickup");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Sliding:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
		{
			if (!base.PlayerManager.Grounded || base.PlayerManager.Background || base.InputManager.Movement.X == 0f || base.PlayerManager.LookingDirection == HorizontalDirection.None)
			{
				break;
			}
			TrileInstance destination = base.PlayerManager.WallCollision.NearLow.Destination;
			if (destination == null || !destination.Trile.ActorSettings.Type.IsPickable() || destination.GetRotatedFace(base.CameraManager.VisibleOrientation) != 0 || destination.Hidden || destination.PhysicsState == null || !destination.PhysicsState.Grounded)
			{
				break;
			}
			NearestTriles nearestTriles = base.LevelManager.NearestTrile(destination.Position);
			if ((nearestTriles.Surface != null && nearestTriles.Surface.Trile.ForceHugging) || (double)Math.Abs(destination.Center.Y - base.PlayerManager.Position.Y) > 0.5)
			{
				break;
			}
			if (destination.Trile.ActorSettings.Type == ActorType.Couch)
			{
				FaceOrientation num = FezMath.OrientationFromPhi(destination.Trile.ActorSettings.Face.ToPhi() + destination.Phi);
				FaceOrientation faceOrientation = base.CameraManager.Viewpoint.GetRotatedView((base.PlayerManager.LookingDirection != HorizontalDirection.Right) ? 1 : (-1)).VisibleOrientation();
				if (num != faceOrientation)
				{
					break;
				}
			}
			base.PlayerManager.Action = ActionType.Grabbing;
			base.PlayerManager.PushedInstance = destination;
			break;
		}
		}
	}

	protected override void Begin()
	{
		pushingDirectionSign = base.PlayerManager.LookingDirection.Sign();
		base.PlayerManager.Velocity *= Vector3.UnitY;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.PushedInstance == null || base.PlayerManager.PushedInstance.Hidden || base.PlayerManager.PushedInstance.PhysicsState == null)
		{
			base.PlayerManager.Action = ActionType.Idle;
			base.PlayerManager.PushedInstance = null;
			return false;
		}
		int frame = base.PlayerManager.Animation.Timing.Frame;
		if (lastFrame != frame && base.PlayerManager.LastAction != ActionType.Pushing && base.PlayerManager.Action == ActionType.Grabbing)
		{
			if (frame == 3)
			{
				grabSound.EmitAt(base.PlayerManager.Position);
			}
			lastFrame = frame;
		}
		Vector3 vector = base.CameraManager.Viewpoint.SideMask();
		Vector3 vector2 = base.CameraManager.Viewpoint.DepthMask();
		Vector3 vector3 = base.CameraManager.Viewpoint.RightVector() * pushingDirectionSign;
		base.PlayerManager.Center = Vector3.Up * base.PlayerManager.Center + (vector2 + vector) * base.PlayerManager.PushedInstance.Center + -vector3 * (base.PlayerManager.PushedInstance.TransformedSize / 2f + base.PlayerManager.Size / 2f);
		if ((base.PlayerManager.Action == ActionType.Pushing || base.PlayerManager.Action == ActionType.Grabbing) && (pushingDirectionSign == -Math.Sign(base.InputManager.Movement.X) || !base.PlayerManager.Grounded))
		{
			base.PlayerManager.PushedInstance = null;
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		if (base.PlayerManager.Action == ActionType.Grabbing && base.PlayerManager.Animation.Timing.Ended && base.InputManager.Movement.X != 0f)
		{
			base.PlayerManager.Action = ActionType.Pushing;
			return false;
		}
		return base.PlayerManager.Action == ActionType.Grabbing;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.Grabbing)
		{
			return type == ActionType.Pushing;
		}
		return true;
	}
}
