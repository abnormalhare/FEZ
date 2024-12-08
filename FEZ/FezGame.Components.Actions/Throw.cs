using System;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Throw : PlayerAction
{
	private static readonly Vector2[] LightOffsets = new Vector2[2]
	{
		new Vector2(-1f, -1f),
		new Vector2(1f, 2f)
	};

	private static readonly Vector2[] HeavyOffsets = new Vector2[4]
	{
		new Vector2(1f, 1f),
		new Vector2(1f, 0f),
		new Vector2(2f, 0f),
		new Vector2(7f, 4f)
	};

	private const float ThrowStrength = 0.08f;

	private bool thrown;

	private SoundEffect throwHeavySound;

	private SoundEffect throwLightSound;

	public Throw(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		throwHeavySound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/ThrowHeavyPickup");
		throwLightSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/ThrowLightPickup");
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
			if (!base.PlayerManager.Background && base.InputManager.GrabThrow == FezButtonState.Pressed && (base.InputManager.Down != FezButtonState.Down || FezMath.AlmostEqual(base.InputManager.Movement, Vector2.Zero, 0.5f)))
			{
				bool flag = base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
				base.PlayerManager.Action = (flag ? ActionType.Throwing : ActionType.ThrowingHeavy);
				thrown = false;
			}
			break;
		case ActionType.CarryEnter:
			break;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		if (base.PlayerManager.CarriedInstance == null)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return;
		}
		if (base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsHeavy())
		{
			throwHeavySound.EmitAt(base.PlayerManager.Position);
		}
		else
		{
			throwLightSound.EmitAt(base.PlayerManager.Position);
		}
		base.GomezService.OnThrowObject();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		base.PlayerManager.Animation.Timing.Update(elapsed);
		if (base.PlayerManager.CarriedInstance != null)
		{
			if (base.PlayerManager.CarriedInstance.PhysicsState == null)
			{
				base.PlayerManager.CarriedInstance = null;
			}
			bool flag = base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
			Vector2[] array = (flag ? LightOffsets : HeavyOffsets);
			if (!flag)
			{
				base.PlayerManager.Velocity *= Vector3.UnitY;
			}
			if (base.PlayerManager.Animation.Timing.Frame < array.Length)
			{
				int frame = base.PlayerManager.Animation.Timing.Frame;
				TrileInstance carriedInstance = base.PlayerManager.CarriedInstance;
				Vector2 vector2 = array[frame];
				Vector3 vector3 = base.PlayerManager.Center + base.PlayerManager.Size / 2f * (Vector3.Down + vector) - carriedInstance.TransformedSize / 2f * vector + carriedInstance.Trile.Size / 2f * (Vector3.UnitY + vector) - vector * 8f / 16f + Vector3.UnitY * 9f / 16f;
				if (flag)
				{
					vector3 += vector * 1f / 16f + Vector3.UnitY * 2f / 16f;
				}
				Vector3 vector4 = vector3 + (vector2.X * vector + vector2.Y * Vector3.Up) * 0.0625f;
				Vector3 velocity = vector4 - carriedInstance.Center;
				carriedInstance.PhysicsState.Velocity = velocity;
				carriedInstance.PhysicsState.UpdatingPhysics = true;
				base.PhysicsManager.Update(carriedInstance.PhysicsState, simple: false, keepInFront: false);
				carriedInstance.PhysicsState.UpdatingPhysics = false;
				carriedInstance.PhysicsState.UpdateInstance();
				carriedInstance.PhysicsState.Velocity = Vector3.Zero;
				base.PlayerManager.Velocity -= vector4 - carriedInstance.Center;
			}
			else if (!thrown)
			{
				thrown = true;
				base.PlayerManager.CarriedInstance.Phi = FezMath.SnapPhi(base.PlayerManager.CarriedInstance.Phi);
				base.PlayerManager.CarriedInstance.PhysicsState.Background = false;
				base.PlayerManager.CarriedInstance.PhysicsState.Velocity = base.PlayerManager.Velocity * 0.5f + (base.PlayerManager.LookingDirection.Sign() * base.CameraManager.Viewpoint.RightVector() + Vector3.Up) * 0.08f;
				base.PlayerManager.CarriedInstance = null;
			}
		}
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			thrown = false;
			base.PlayerManager.SyncCollisionSize();
			base.PlayerManager.Action = ActionType.Idle;
		}
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.Throwing)
		{
			return type == ActionType.ThrowingHeavy;
		}
		return true;
	}
}
