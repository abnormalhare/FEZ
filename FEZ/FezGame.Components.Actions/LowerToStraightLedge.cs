using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class LowerToStraightLedge : PlayerAction
{
	private Vector3 camOrigin;

	private SoundEffect sound;

	private SoundEffect sLowerToLedge;

	public LowerToStraightLedge(Game game)
		: base(game)
	{
		base.UpdateOrder = 3;
	}

	protected override void LoadContent()
	{
		sound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeGrab");
		sLowerToLedge = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LowerToLedge");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.LookingLeft:
		case ActionType.LookingRight:
		case ActionType.LookingUp:
		case ActionType.LookingDown:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Sliding:
		case ActionType.Landing:
		{
			if (base.PlayerManager.Background || ((base.InputManager.Jump != FezButtonState.Pressed || !base.InputManager.Down.IsDown()) && (!base.PlayerManager.Action.IsOnLedge() || base.InputManager.Down != FezButtonState.Pressed)) || !base.PlayerManager.Grounded || base.PlayerManager.Ground.First.GetRotatedFace(base.CameraManager.Viewpoint.VisibleOrientation()) != CollisionType.TopOnly || !FezMath.AlmostEqual(base.InputManager.Movement.X, 0f))
			{
				break;
			}
			TrileInstance trileInstance = base.PlayerManager.Ground.NearLow ?? base.PlayerManager.Ground.FarHigh;
			if (base.CollisionManager.CollideEdge(trileInstance.Center + trileInstance.TransformedSize * Vector3.UnitY * 0.498f, Vector3.Down * Math.Sign(base.CollisionManager.GravityFactor), base.PlayerManager.Size * FezMath.XZMask / 2f, Direction2D.Vertical).AnyHit())
			{
				base.PlayerManager.Position -= Vector3.UnitY * 0.01f * Math.Sign(base.CollisionManager.GravityFactor);
				base.PlayerManager.Velocity -= 0.0075000003f * Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
				break;
			}
			if (base.PlayerManager.Grounded)
			{
				TrileInstance surface = base.LevelManager.NearestTrile(base.PlayerManager.Ground.First.Center, QueryOptions.None).Surface;
				if (surface != null && surface.Trile.ActorSettings.Type == ActorType.Ladder)
				{
					break;
				}
			}
			base.PlayerManager.HeldInstance = base.PlayerManager.Ground.NearLow;
			base.CameraManager.Viewpoint.SideMask();
			base.PlayerManager.Velocity = Vector3.Zero;
			base.PlayerManager.Action = ActionType.LowerToLedge;
			Waiters.Wait(0.3, delegate
			{
				if (base.PlayerManager.Action == ActionType.LowerToLedge)
				{
					sound.EmitAt(base.PlayerManager.Position);
					base.PlayerManager.Velocity = Vector3.Zero;
				}
			});
			camOrigin = base.CameraManager.Center;
			break;
		}
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.HeldInstance == null)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		if (base.PlayerManager.HeldInstance.PhysicsState != null)
		{
			camOrigin += base.PlayerManager.HeldInstance.PhysicsState.Velocity;
		}
		Vector3 vector = base.CameraManager.Viewpoint.SideMask();
		Vector3 vector2 = base.CameraManager.Viewpoint.DepthMask();
		Vector3 vector3 = base.CameraManager.Viewpoint.ForwardVector();
		base.PlayerManager.Position = base.PlayerManager.Position * vector + base.PlayerManager.HeldInstance.Center * (Vector3.UnitY + vector2) + vector3 * (0f - (0.5f + base.PlayerManager.Size.X / 2f)) + base.PlayerManager.HeldInstance.Trile.Size.Y / 2f * Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
		base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + base.PlayerManager.HeldInstance.Center * vector2 + vector3 * -(base.PlayerManager.HeldInstance.TransformedSize / 2f + base.PlayerManager.Size.X * vector2 / 4f);
		base.PhysicsManager.HugWalls(base.PlayerManager, determineBackground: false, postRotation: false, keepInFront: true);
		Vector3 vector4 = base.PlayerManager.Size.Y / 2f * Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
		if (!base.CameraManager.StickyCam && !base.CameraManager.Constrained)
		{
			base.CameraManager.Center = Vector3.Lerp(camOrigin, camOrigin - vector4, base.PlayerManager.Animation.Timing.NormalizedStep);
		}
		base.PlayerManager.SplitUpCubeCollectorOffset = vector4 * (1f - base.PlayerManager.Animation.Timing.NormalizedStep);
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.SplitUpCubeCollectorOffset = Vector3.Zero;
			base.PlayerManager.Action = ActionType.GrabLedgeBack;
			return false;
		}
		return true;
	}

	protected override void Begin()
	{
		sLowerToLedge.EmitAt(base.PlayerManager.Position);
		base.Begin();
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.LowerToLedge;
	}
}
