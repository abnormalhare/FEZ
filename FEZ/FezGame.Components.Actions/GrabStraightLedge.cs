using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class GrabStraightLedge : PlayerAction
{
	private Viewpoint? rotatedFrom;

	private SoundEffect sound;

	protected override bool ViewTransitionIndependent => true;

	public GrabStraightLedge(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		sound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeGrab");
		base.CameraManager.ViewpointChanged += delegate
		{
			if (IsActionAllowed(base.PlayerManager.Action) && base.CameraManager.Viewpoint.IsOrthographic() && base.CameraManager.Viewpoint != base.CameraManager.LastViewpoint && !base.PlayerManager.IsOnRotato && !base.PlayerManager.FreshlyRespawned)
			{
				if (rotatedFrom.HasValue && rotatedFrom.Value == base.CameraManager.Viewpoint)
				{
					rotatedFrom = null;
				}
				else if (!rotatedFrom.HasValue)
				{
					rotatedFrom = base.CameraManager.LastViewpoint;
				}
			}
		};
	}

	protected override void TestConditions()
	{
		ActionType action = base.PlayerManager.Action;
		if ((action != ActionType.Jumping && action != ActionType.Falling) || !base.InputManager.Up.IsDown())
		{
			return;
		}
		FaceOrientation face = base.CameraManager.Viewpoint.VisibleOrientation();
		TrileInstance deep = base.PlayerManager.AxisCollision[VerticalDirection.Up].Deep;
		TrileInstance deep2 = base.PlayerManager.AxisCollision[VerticalDirection.Down].Deep;
		if ((deep != null && deep.GetRotatedFace(face) == CollisionType.AllSides) || deep2 == null || deep2.GetRotatedFace(face) != CollisionType.TopOnly || base.CollisionManager.CollideEdge(deep2.Center, Vector3.Down * Math.Sign(base.CollisionManager.GravityFactor), base.PlayerManager.Size * FezMath.XZMask / 2f, Direction2D.Vertical).AnyHit())
		{
			return;
		}
		TrileInstance surface = base.PlayerManager.AxisCollision[VerticalDirection.Down].Surface;
		if ((surface == null || !surface.Trile.ActorSettings.Type.IsClimbable()) && deep2.Enabled && (base.PlayerManager.Action != ActionType.Jumping || !((double)((deep2.Center - base.PlayerManager.LeaveGroundPosition) * base.CameraManager.Viewpoint.ScreenSpaceMask()).Length() < 1.25)))
		{
			base.PlayerManager.Action = ActionType.GrabLedgeBack;
			Vector3 vector = base.CameraManager.Viewpoint.DepthMask();
			Vector3 vector2 = base.CameraManager.Viewpoint.SideMask();
			Vector3 vector3 = base.CameraManager.Viewpoint.ForwardVector();
			base.PlayerManager.HeldInstance = deep2;
			base.PlayerManager.Velocity *= vector2 * 0.5f;
			base.PlayerManager.Position = base.PlayerManager.Position * vector2 + deep2.Center * (Vector3.UnitY + vector) + vector3 * -(base.PlayerManager.HeldInstance.TransformedSize / 2f + base.PlayerManager.Size.X * vector / 4f) + base.PlayerManager.HeldInstance.Trile.Size.Y / 2f * Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
			Waiters.Wait(0.1, delegate
			{
				sound.EmitAt(base.PlayerManager.Position);
				base.PlayerManager.Velocity = Vector3.Zero;
			});
		}
	}

	protected override void Begin()
	{
		base.Begin();
		base.GomezService.OnGrabLedge();
	}

	public override void Update(GameTime gameTime)
	{
		if (rotatedFrom.HasValue && (double)base.CameraManager.ViewTransitionStep >= 0.6)
		{
			int distance = base.CameraManager.Viewpoint.GetDistance(rotatedFrom.Value);
			if (Math.Abs(distance) % 2 == 0)
			{
				base.PlayerManager.Background = !base.PlayerManager.Background;
				base.PlayerManager.Action = (base.PlayerManager.Action.FacesBack() ? ActionType.GrabLedgeFront : ActionType.GrabLedgeBack);
			}
			else
			{
				if (base.PlayerManager.Action.FacesBack())
				{
					base.PlayerManager.LookingDirection = ((Math.Sign(distance) > 0) ? HorizontalDirection.Left : HorizontalDirection.Right);
				}
				else
				{
					base.PlayerManager.LookingDirection = ((Math.Sign(distance) <= 0) ? HorizontalDirection.Left : HorizontalDirection.Right);
				}
				base.PlayerManager.Action = ActionType.GrabCornerLedge;
				base.PlayerManager.Position += base.PlayerManager.Size.Z / 4f * rotatedFrom.Value.ForwardVector();
				base.PlayerManager.Background = false;
			}
			SyncAnimation(isActive: true);
			rotatedFrom = null;
		}
		base.Update(gameTime);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		base.PlayerManager.Velocity *= 0.85f;
		if (base.PlayerManager.HeldInstance.PhysicsState != null && base.CameraManager.ActionRunning)
		{
			base.PlayerManager.Position += base.PlayerManager.HeldInstance.PhysicsState.Velocity;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.GrabLedgeFront)
		{
			return type == ActionType.GrabLedgeBack;
		}
		return true;
	}
}
