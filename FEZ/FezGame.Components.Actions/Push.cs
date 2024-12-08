using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Push : PlayerAction
{
	private readonly MovementHelper movementHelper = new MovementHelper(1.8799999f, 0f, float.MaxValue);

	private TrileGroup pickupGroup;

	private SoundEffect sCratePush;

	private SoundEffect sGomezPush;

	private SoundEmitter eCratePush;

	private SoundEmitter eGomezPush;

	public Push(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sCratePush = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/PushPickup");
		sGomezPush = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/GomezPush");
	}

	protected override void Begin()
	{
		base.Begin();
		if (eCratePush == null || eCratePush.Dead)
		{
			eCratePush = sCratePush.EmitAt(base.PlayerManager.PushedInstance.Center, loop: true);
		}
		else
		{
			eCratePush.Cue.Resume();
		}
		if (eGomezPush == null || eGomezPush.Dead)
		{
			eGomezPush = sGomezPush.EmitAt(base.PlayerManager.Position, loop: true);
		}
		else
		{
			eGomezPush.Cue.Resume();
		}
		if (!base.LevelManager.PickupGroups.TryGetValue(base.PlayerManager.PushedInstance, out pickupGroup))
		{
			pickupGroup = null;
		}
	}

	protected override void TestConditions()
	{
		base.TestConditions();
		if (base.PlayerManager.Action != ActionType.Pushing && eCratePush != null && !eCratePush.Dead && eCratePush.Cue.State != SoundState.Paused)
		{
			eCratePush.Cue.Pause();
		}
		if (base.PlayerManager.Action != ActionType.Pushing && eGomezPush != null && !eGomezPush.Dead && eGomezPush.Cue.State != SoundState.Paused)
		{
			eGomezPush.Cue.Pause();
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.PushedInstance == null || base.PlayerManager.PushedInstance.Hidden || base.PlayerManager.PushedInstance.PhysicsState == null)
		{
			base.PlayerManager.Action = ActionType.Idle;
			base.PlayerManager.PushedInstance = null;
			return false;
		}
		Vector3 vector = base.CameraManager.Viewpoint.SideMask();
		Vector3 vector2 = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		TrileInstance pushedInstance = base.PlayerManager.PushedInstance;
		InstancePhysicsState physicsState = pushedInstance.PhysicsState;
		eCratePush.Position = pushedInstance.Center;
		eGomezPush.Position = base.PlayerManager.Center;
		if (!physicsState.Grounded)
		{
			base.PlayerManager.PushedInstance = null;
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		int num = FindStackSize(pushedInstance, 0);
		if (num <= 2)
		{
			movementHelper.Entity = physicsState;
			float num2 = base.InputManager.Movement.X;
			if (physicsState.WallCollision.AnyCollided() && physicsState.WallCollision.First.Destination.Trile.ActorSettings.Type.IsPickable())
			{
				num2 *= 5f;
			}
			if (pushedInstance.Trile.ActorSettings.Type == ActorType.Couch)
			{
				num2 *= 2f;
			}
			movementHelper.Update((float)elapsed.TotalSeconds, num2 / (float)(num + 1));
			if (pickupGroup != null)
			{
				pushedInstance.PhysicsState.Puppet = false;
				foreach (TrileInstance trile in pickupGroup.Triles)
				{
					if (trile != pushedInstance)
					{
						trile.PhysicsState.Velocity = pushedInstance.PhysicsState.Velocity;
						trile.PhysicsState.Puppet = true;
					}
				}
			}
			Vector3 vector3 = base.CameraManager.Viewpoint.DepthMask();
			base.PlayerManager.Center = Vector3.Up * base.PlayerManager.Center + (vector3 + vector) * physicsState.Center + -vector2 * (pushedInstance.TransformedSize / 2f + base.PlayerManager.Size / 2f);
			eCratePush.VolumeFactor = FezMath.Saturate(Math.Abs(physicsState.Velocity.Dot(vector)) / 0.024f);
			if (FezMath.AlmostEqual(physicsState.Velocity.Dot(vector), 0f))
			{
				base.PlayerManager.Action = ActionType.Grabbing;
				return false;
			}
		}
		else
		{
			base.PlayerManager.Action = ActionType.Grabbing;
			if (!eCratePush.Dead)
			{
				eCratePush.Cue.Pause();
			}
			if (!eGomezPush.Dead)
			{
				eGomezPush.Cue.Pause();
			}
		}
		return base.PlayerManager.Action == ActionType.Pushing;
	}

	private int FindStackSize(TrileInstance instance, int stackSize)
	{
		Vector3 halfSize = instance.TransformedSize / 2f - new Vector3(0.004f);
		MultipleHits<CollisionResult> result = base.CollisionManager.CollideEdge(instance.Center, instance.Trile.Size.Y * Vector3.Up, halfSize, Direction2D.Vertical);
		if (result.AnyCollided())
		{
			TrileInstance destination = result.First.Destination;
			if (destination.PhysicsState != null && destination.PhysicsState.Grounded)
			{
				return FindStackSize(destination, stackSize + 1);
			}
		}
		return stackSize;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Pushing;
	}
}
