using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class DropTrile : PlayerAction
{
	private SoundEffect dropHeavySound;

	private SoundEffect dropLightSound;

	public DropTrile(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		dropHeavySound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/DropHeavyPickup");
		dropLightSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/DropLightPickup");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.CarryIdle:
		case ActionType.CarryWalk:
		case ActionType.CarryJump:
		case ActionType.CarrySlide:
		case ActionType.CarryHeavyIdle:
		case ActionType.CarryHeavyWalk:
		case ActionType.CarryHeavyJump:
		case ActionType.CarryHeavySlide:
		{
			if (base.PlayerManager.Background || base.InputManager.GrabThrow != FezButtonState.Pressed || (!(base.InputManager.Movement.Y < -0.5f) && !((double)Math.Abs(base.InputManager.Movement.X) < 0.25)))
			{
				break;
			}
			TrileInstance carriedInstance = base.PlayerManager.CarriedInstance;
			bool flag = carriedInstance.Trile.ActorSettings.Type.IsLight();
			base.PlayerManager.Action = (flag ? ActionType.DropTrile : ActionType.DropHeavyTrile);
			Vector3 vector = base.CameraManager.Viewpoint.SideMask();
			Vector3 vector2 = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
			Vector3 vector3 = base.PlayerManager.Center + base.PlayerManager.Size / 2f * (Vector3.Down + vector2) - carriedInstance.TransformedSize / 2f * vector2 + carriedInstance.Trile.Size / 2f * (Vector3.UnitY + vector2) + 0.125f * vector2;
			carriedInstance.Enabled = false;
			MultipleHits<CollisionResult> result = base.CollisionManager.CollideEdge(carriedInstance.Center, vector3 - carriedInstance.Center, carriedInstance.TransformedSize / 2f, Direction2D.Horizontal);
			if (result.AnyCollided())
			{
				CollisionResult collisionResult = result.NearLow;
				if (!collisionResult.Collided || collisionResult.Destination.GetRotatedFace(base.CameraManager.VisibleOrientation) != 0 || Math.Abs(collisionResult.Destination.Center.Y - vector3.Y) >= 1f)
				{
					collisionResult = result.FarHigh;
				}
				if (collisionResult.Collided && collisionResult.Destination.GetRotatedFace(base.CameraManager.VisibleOrientation) == CollisionType.AllSides && Math.Abs(collisionResult.Destination.Center.Y - vector3.Y) < 1f)
				{
					TrileInstance destination = collisionResult.Destination;
					Vector3 vector4 = destination.Center - vector2 * destination.TransformedSize / 2f;
					Vector3 vector5 = vector3 + vector2 * carriedInstance.TransformedSize / 2f;
					Vector3 vector6 = vector * (vector5 - vector4);
					base.PlayerManager.Position -= vector6;
				}
			}
			carriedInstance.Enabled = true;
			base.PlayerManager.Velocity *= Vector3.UnitY;
			break;
		}
		case ActionType.CarryEnter:
			break;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		if (base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsHeavy())
		{
			dropHeavySound.EmitAt(base.PlayerManager.Position);
		}
		else
		{
			dropLightSound.EmitAt(base.PlayerManager.Position);
		}
		base.GomezService.OnDropObject();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.CarriedInstance == null)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		TrileInstance carriedInstance = base.PlayerManager.CarriedInstance;
		Vector3 vector2 = base.PlayerManager.Center + base.PlayerManager.Size / 2f * (Vector3.Down + vector) - carriedInstance.TransformedSize / 2f * vector + carriedInstance.Trile.Size / 2f * (Vector3.UnitY + vector) + 0.125f * vector;
		bool flag = carriedInstance.Trile.ActorSettings.Type.IsLight();
		Vector2[] array = (flag ? Lift.LightTrilePositioning : Lift.HeavyTrilePositioning);
		int num = (flag ? 4 : 7) - base.PlayerManager.Animation.Timing.Frame;
		Vector3 vector3 = vector2 + (array[num].X * -vector + array[num].Y * Vector3.Up) * 1f / 16f;
		carriedInstance.PhysicsState.Center = vector3;
		carriedInstance.PhysicsState.UpdateInstance();
		base.PlayerManager.Position -= vector3 - carriedInstance.Center;
		base.PlayerManager.CarriedInstance.PhysicsState.UpdateInstance();
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			Vector3 vector4 = 3.15f * (float)Math.Sign(base.CollisionManager.GravityFactor) * 0.15f * (float)elapsed.TotalSeconds * Vector3.Down;
			if (base.PlayerManager.GroundMovement.Y < 0f)
			{
				vector4 += base.PlayerManager.GroundMovement;
			}
			MultipleHits<CollisionResult> result = base.CollisionManager.CollideEdge(carriedInstance.PhysicsState.Center, vector4, carriedInstance.TransformedSize / 2f, Direction2D.Vertical);
			if (result.AnyCollided())
			{
				carriedInstance.PhysicsState.Ground = new MultipleHits<TrileInstance>
				{
					NearLow = (result.NearLow.Collided ? result.NearLow.Destination : null),
					FarHigh = (result.FarHigh.Collided ? result.FarHigh.Destination : null)
				};
				if (carriedInstance.PhysicsState.Ground.First.PhysicsState != null)
				{
					carriedInstance.PhysicsState.GroundMovement = carriedInstance.PhysicsState.Ground.First.PhysicsState.Velocity;
					carriedInstance.PhysicsState.Center += carriedInstance.PhysicsState.GroundMovement;
				}
			}
			carriedInstance.PhysicsState.Velocity = vector4;
			carriedInstance.PhysicsState.UpdateInstance();
			if (flag)
			{
				base.PlayerManager.Action = ActionType.Idle;
			}
			else
			{
				base.PlayerManager.PushedInstance = base.PlayerManager.CarriedInstance;
				base.PlayerManager.Action = ActionType.Grabbing;
			}
			base.PlayerManager.CarriedInstance = null;
			base.PhysicsManager.HugWalls(base.PlayerManager, determineBackground: false, postRotation: false, keepInFront: true);
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.DropTrile)
		{
			return type == ActionType.DropHeavyTrile;
		}
		return true;
	}
}
