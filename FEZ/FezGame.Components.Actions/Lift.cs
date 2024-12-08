using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Lift : PlayerAction
{
	public static readonly Vector2[] LightTrilePositioning = new Vector2[8]
	{
		Vector2.Zero,
		Vector2.Zero,
		new Vector2(1f, 2f),
		new Vector2(4f, 9f),
		new Vector2(8f, 14f),
		new Vector2(9f, 14f),
		new Vector2(10f, 10f),
		new Vector2(10f, 11f)
	};

	public static readonly Vector2[] HeavyTrilePositioning = new Vector2[10]
	{
		Vector2.Zero,
		Vector2.Zero,
		Vector2.Zero,
		new Vector2(1f, 1f),
		new Vector2(2f, 3f),
		new Vector2(4f, 7f),
		new Vector2(7f, 12f),
		new Vector2(8f, 13f),
		new Vector2(10f, 9f),
		new Vector2(11f, 10f)
	};

	private SoundEffect liftHeavySound;

	private SoundEffect liftLightSound;

	public Lift(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		liftHeavySound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LiftHeavyPickup");
		liftLightSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LiftLightPickup");
	}

	protected override void TestConditions()
	{
		if (base.CollisionManager.GravityFactor < 0f)
		{
			return;
		}
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Sliding:
		case ActionType.Landing:
		case ActionType.Grabbing:
		case ActionType.Pushing:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
		{
			if (base.PlayerManager.Background || !base.PlayerManager.Grounded || base.InputManager.GrabThrow != FezButtonState.Pressed)
			{
				break;
			}
			TrileInstance trileInstance = base.PlayerManager.PushedInstance ?? base.PlayerManager.AxisCollision[VerticalDirection.Up].Deep ?? base.PlayerManager.AxisCollision[VerticalDirection.Down].Deep;
			if (trileInstance == null || !trileInstance.Trile.ActorSettings.Type.IsPickable() || trileInstance.Trile.ActorSettings.Type == ActorType.Couch || !trileInstance.PhysicsState.Grounded)
			{
				break;
			}
			Vector3 halfSize = trileInstance.TransformedSize / 2f - new Vector3(0.004f);
			if (base.CollisionManager.CollideEdge(trileInstance.Center, trileInstance.Trile.Size.Y * Vector3.Up * Math.Sign(base.CollisionManager.GravityFactor), halfSize, Direction2D.Vertical).AnyCollided())
			{
				break;
			}
			TrileInstance trileInstance2 = base.LevelManager.ActualInstanceAt(trileInstance.Center + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor));
			if ((trileInstance2 == null || trileInstance2.PhysicsState == null || trileInstance2.PhysicsState.Ground.First != trileInstance) && (!base.LevelManager.PickupGroups.TryGetValue(trileInstance, out var value) || value.Triles.Count <= 1))
			{
				ActionType actionType = (trileInstance.Trile.ActorSettings.Type.IsLight() ? ActionType.Lifting : ActionType.LiftingHeavy);
				if (base.PlayerManager.Action == ActionType.Grabbing)
				{
					base.PlayerManager.CarriedInstance = trileInstance;
					base.PlayerManager.Action = actionType;
					break;
				}
				base.PlayerManager.PushedInstance = trileInstance;
				base.WalkTo.Destination = GetDestination;
				base.WalkTo.NextAction = actionType;
				base.PlayerManager.Action = ActionType.WalkingTo;
			}
			break;
		}
		}
	}

	private Vector3 GetDestination()
	{
		TrileInstance pushedInstance = base.PlayerManager.PushedInstance;
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		Vector3 vector2 = vector.Abs();
		Vector3 vector3 = base.CameraManager.Viewpoint.DepthMask();
		return (pushedInstance.Center * vector2 + base.PlayerManager.Position * vector3) * FezMath.XZMask + -pushedInstance.TransformedSize / 2f * vector + -0.4375f * vector + base.PlayerManager.Position * Vector3.UnitY;
	}

	protected override void Begin()
	{
		if (base.PlayerManager.CarriedInstance == null && base.PlayerManager.PushedInstance == null)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return;
		}
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		Vector3 mask = base.CameraManager.Viewpoint.VisibleAxis().GetMask();
		Vector3 vector2 = vector.Abs();
		if (base.PlayerManager.PushedInstance != null)
		{
			base.PlayerManager.CarriedInstance = base.PlayerManager.PushedInstance;
			base.PlayerManager.PushedInstance = null;
		}
		TrileInstance carriedInstance = base.PlayerManager.CarriedInstance;
		TrileInstance first = base.PlayerManager.Ground.First;
		Vector3 vector3 = carriedInstance.Center * vector2 + base.PlayerManager.Position * mask + (first.Center.Y + (first.Trile.Size.Y / 2f + carriedInstance.Trile.Size.Y / 2f) * (float)Math.Sign(base.CollisionManager.GravityFactor)) * Vector3.UnitY;
		base.PlayerManager.CarriedInstance.PhysicsState.Center = vector3;
		base.PlayerManager.CarriedInstance.PhysicsState.UpdateInstance();
		base.PlayerManager.Position = base.PlayerManager.Position * (Vector3.One - vector2) + vector3 * vector2 + -carriedInstance.TransformedSize / 2f * vector + -0.1875f * vector;
		if (base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsHeavy())
		{
			liftHeavySound.EmitAt(base.PlayerManager.Position);
		}
		else
		{
			liftLightSound.EmitAt(base.PlayerManager.Position);
		}
		base.GomezService.OnLiftObject();
		base.PlayerManager.Velocity *= Vector3.UnitY;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.CarriedInstance == null)
		{
			return false;
		}
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		Vector3 vector2 = base.PlayerManager.Center + base.PlayerManager.Size / 2f * (Vector3.Down * Math.Sign(base.CollisionManager.GravityFactor) + vector) - base.PlayerManager.CarriedInstance.TransformedSize / 2f * vector + base.PlayerManager.CarriedInstance.Trile.Size / 2f * (Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor) + vector);
		bool flag = base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
		base.PlayerManager.Animation.Timing.Update(elapsed);
		int frame = base.PlayerManager.Animation.Timing.Frame;
		Vector2[] array = (flag ? LightTrilePositioning : HeavyTrilePositioning);
		base.PlayerManager.CarriedInstance.PhysicsState.Center = vector2 + (array[frame].X * -vector + array[frame].Y * Vector3.Up * Math.Sign(base.CollisionManager.GravityFactor)) * 0.0625f + 0.1875f * vector;
		base.PlayerManager.CarriedInstance.PhysicsState.UpdateInstance();
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Action = (flag ? ActionType.CarryIdle : ActionType.CarryHeavyIdle);
		}
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.Lifting)
		{
			return type == ActionType.LiftingHeavy;
		}
		return true;
	}
}
