using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Jump : PlayerAction
{
	public const float SideJumpStrength = 0.25f;

	public const float UpJumpStrength = 1.025f;

	private SoundEffect jumpSound;

	private TimeSpan sinceJumped;

	private bool scheduleJump;

	public Jump(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		jumpSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Jump");
	}

	protected override void TestConditions()
	{
		if ((!FezMath.In(base.PlayerManager.Action, ActionType.Sliding, ActionType.GrabCornerLedge, ActionType.Running, ActionType.RunTurnAround, ActionType.Walking, ActionType.Landing, ActionType.WalkingTo, ActionType.GrabTombstone, ActionTypeComparer.Default) && !base.PlayerManager.Climbing && !base.PlayerManager.Swimming && !base.PlayerManager.Action.IsIdle() && (base.PlayerManager.Action != ActionType.Falling || !base.PlayerManager.CanDoubleJump) && base.PlayerManager.Action != ActionType.Grabbing && base.PlayerManager.Action != ActionType.Pushing && !base.PlayerManager.Action.IsLookingAround()) || (base.InputManager.Jump != FezButtonState.Pressed && ((!base.PlayerManager.Grounded && !base.PlayerManager.Action.IsOnLedge()) || !((double)(base.PlayerManager.Velocity.Y * (float)Math.Sign(base.CollisionManager.GravityFactor)) > 0.1))))
		{
			return;
		}
		base.PlayerManager.PushedInstance = null;
		if (base.PlayerManager.CanDoubleJump)
		{
			base.PlayerManager.CanDoubleJump = false;
		}
		if (base.InputManager.Down.IsDown() && ((base.PlayerManager.Grounded && base.PlayerManager.Ground.First.GetRotatedFace(base.CameraManager.Viewpoint.VisibleOrientation()) == CollisionType.TopOnly) || base.PlayerManager.Climbing))
		{
			return;
		}
		if (base.PlayerManager.Action == ActionType.GrabCornerLedge)
		{
			HorizontalDirection horizontalDirection = FezMath.DirectionFromMovement(base.InputManager.Movement.X);
			if (horizontalDirection == HorizontalDirection.None || horizontalDirection == base.PlayerManager.LookingDirection)
			{
				return;
			}
			Vector3 position = base.PlayerManager.Position;
			base.PlayerManager.Position += base.CameraManager.Viewpoint.RightVector() * -base.PlayerManager.LookingDirection.Sign();
			base.PhysicsManager.DetermineInBackground(base.PlayerManager, allowEnterInBackground: true, viewpointChanged: false, keepInFront: false);
			base.PlayerManager.Position = position;
		}
		if (base.InputManager.Jump == FezButtonState.Pressed)
		{
			sinceJumped = TimeSpan.Zero;
			scheduleJump = true;
		}
		else
		{
			DoJump();
		}
		base.PlayerManager.Action = ActionType.Jumping;
	}

	private void DoJump()
	{
		bool flag = base.PlayerManager.LastAction.IsClimbingLadder() || base.PlayerManager.LastAction.IsClimbingVine() || base.PlayerManager.HeldInstance != null;
		if (flag)
		{
			base.PlayerManager.Velocity += base.CameraManager.Viewpoint.RightVector() * base.InputManager.Movement.X * 0.25f;
		}
		base.PlayerManager.HeldInstance = null;
		if (scheduleJump || base.InputManager.Jump == FezButtonState.Pressed)
		{
			jumpSound.EmitAt(base.PlayerManager.Position);
		}
		float gravityFactor = base.CollisionManager.GravityFactor;
		gravityFactor = (1.325f + Math.Abs(gravityFactor) * 0.675f) / 2f * (float)Math.Sign(gravityFactor);
		base.PlayerManager.Velocity *= FezMath.XZMask;
		Vector3 vector = 0.15f * gravityFactor * 1.025f * ((flag || base.PlayerManager.Swimming) ? 0.775f : 1f) * Vector3.UnitY;
		base.PlayerManager.Velocity += vector;
		sinceJumped = TimeSpan.Zero;
		base.GomezService.OnJump();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		sinceJumped += elapsed;
		if (scheduleJump && sinceJumped.TotalMilliseconds >= 60.0)
		{
			DoJump();
			scheduleJump = false;
		}
		if (base.PlayerManager.Grounded)
		{
			WalkRun.MovementHelper.Update((float)elapsed.TotalSeconds);
		}
		else if (base.InputManager.Jump == FezButtonState.Down)
		{
			float num = ((sinceJumped.TotalSeconds < 0.25) ? 0.6f : 0f);
			float gravityFactor = base.CollisionManager.GravityFactor;
			gravityFactor = (1.2f + Math.Abs(gravityFactor) * 0.8f) / 2f * (float)Math.Sign(gravityFactor);
			Vector3 vector = (float)elapsed.TotalSeconds * num * gravityFactor * 1.025f / 2f * Vector3.UnitY;
			base.PlayerManager.Velocity += vector;
		}
		if (((base.CollisionManager.GravityFactor < 0f) ? (base.PlayerManager.Velocity.Y >= 0f) : (base.PlayerManager.Velocity.Y <= 0f)) && !base.PlayerManager.Grounded && !scheduleJump)
		{
			base.PlayerManager.Action = ActionType.Falling;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Jumping;
	}
}
