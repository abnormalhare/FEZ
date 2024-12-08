using System;
using FezEngine;
using FezEngine.Tools;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

internal class WalkTo : PlayerAction, IWalkToService
{
	private readonly MovementHelper movementHelper = new MovementHelper(4.7f, 5.875f, 0.2f);

	private HorizontalDirection originalLookingDirection;

	private bool stoppedByWall;

	public Func<Vector3> Destination { get; set; }

	public ActionType NextAction { get; set; }

	public WalkTo(Game game)
		: base(game)
	{
	}

	protected override void Begin()
	{
		originalLookingDirection = base.PlayerManager.LookingDirection;
		movementHelper.Entity = base.PlayerManager;
		stoppedByWall = false;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		float timeFactor = (movementHelper.Running ? 1.25f : 1f);
		base.PlayerManager.Animation.Timing.Update(elapsed, timeFactor);
		base.PlayerManager.LookingDirection = originalLookingDirection;
		float num = (Destination() - base.PlayerManager.Position).Dot(base.CameraManager.Viewpoint.RightVector());
		int num2 = ((!(num < 0f)) ? 1 : (-1));
		base.PlayerManager.LookingDirection = ((num < 0f) ? HorizontalDirection.Left : HorizontalDirection.Right);
		stoppedByWall = base.PlayerManager.WallCollision.AnyCollided();
		if (FezMath.AlmostEqual(num, 0.0, 0.01) || stoppedByWall)
		{
			ChangeAction();
		}
		else
		{
			movementHelper.Update((float)elapsed.TotalSeconds, (float)num2 * 0.75f);
			float value = base.PlayerManager.Velocity.Dot(base.CameraManager.Viewpoint.SideMask());
			base.PlayerManager.Velocity = base.PlayerManager.Velocity * (Vector3.UnitY + base.CameraManager.Viewpoint.DepthMask()) + base.CameraManager.Viewpoint.RightVector() * Math.Min(Math.Abs(value), Math.Abs(num)) * num2;
		}
		return false;
	}

	private void ChangeAction()
	{
		base.PlayerManager.LookingDirection = originalLookingDirection;
		base.PlayerManager.Action = NextAction;
		if (!stoppedByWall)
		{
			base.PlayerManager.Position = Destination();
			base.PhysicsManager.HugWalls(base.PlayerManager, determineBackground: false, postRotation: false, keepInFront: true);
		}
		base.PlayerManager.Velocity *= Vector3.UnitY;
		Destination = null;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.WalkingTo;
	}
}
