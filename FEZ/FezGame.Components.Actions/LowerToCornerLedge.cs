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

public class LowerToCornerLedge : PlayerAction
{
	private Vector3 camOrigin;

	private Vector3 playerOrigin;

	private SoundEffect sound;

	private SoundEffect sLowerToLedge;

	public LowerToCornerLedge(Game game)
		: base(game)
	{
		base.UpdateOrder = 3;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeGrab");
		sLowerToLedge = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LowerToLedge");
	}

	protected override void TestConditions()
	{
		if (base.PlayerManager.Background)
		{
			return;
		}
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
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
		{
			if (!base.PlayerManager.Grounded || base.InputManager.Down != FezButtonState.Pressed)
			{
				break;
			}
			TrileInstance nearLow = base.PlayerManager.Ground.NearLow;
			TrileInstance farHigh = base.PlayerManager.Ground.FarHigh;
			Trile trile = nearLow?.Trile;
			Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
			TrileInstance trileInstance = base.PlayerManager.Ground.NearLow ?? base.PlayerManager.Ground.FarHigh;
			if (!base.CollisionManager.CollideEdge(trileInstance.Center + trileInstance.TransformedSize * (Vector3.UnitY * 0.498f + vector * 0.5f), Vector3.Down * Math.Sign(base.CollisionManager.GravityFactor), base.PlayerManager.Size * FezMath.XZMask / 2f, Direction2D.Vertical).AnyHit() && nearLow != null && nearLow.GetRotatedFace(base.CameraManager.VisibleOrientation) != CollisionType.None && trile.ActorSettings.Type != ActorType.Ladder && nearLow != farHigh && (farHigh == null || farHigh.GetRotatedFace(base.CameraManager.VisibleOrientation) == CollisionType.None))
			{
				TrileInstance trileInstance2 = base.LevelManager.ActualInstanceAt(nearLow.Position + vector + new Vector3(0.5f));
				TrileInstance deep = base.LevelManager.NearestTrile(nearLow.Position + vector + new Vector3(0.5f)).Deep;
				if ((deep == null || !deep.Enabled || deep.GetRotatedFace(base.CameraManager.VisibleOrientation) == CollisionType.None) && (trileInstance2 == null || !trileInstance2.Enabled || trileInstance2.Trile.Immaterial || trileInstance2.Trile.ActorSettings.Type == ActorType.Vine))
				{
					base.WalkTo.Destination = GetDestination;
					base.WalkTo.NextAction = ActionType.LowerToCornerLedge;
					base.PlayerManager.Action = ActionType.WalkingTo;
					base.PlayerManager.HeldInstance = nearLow;
				}
			}
			break;
		}
		}
	}

	protected override void Begin()
	{
		base.Begin();
		base.PlayerManager.Velocity = Vector3.Zero;
		camOrigin = base.CameraManager.Center;
		sLowerToLedge.EmitAt(base.PlayerManager.Position);
		Waiters.Wait(0.5799999833106995, delegate
		{
			sound.EmitAt(base.PlayerManager.Position);
		});
	}

	private Vector3 GetDestination()
	{
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		if (base.PlayerManager.Action == ActionType.LowerToCornerLedge)
		{
			playerOrigin = base.PlayerManager.Position;
			Vector3 vector2 = base.PlayerManager.HeldInstance.Center + (vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f;
			base.PlayerManager.SplitUpCubeCollectorOffset = playerOrigin - vector2;
			return vector2;
		}
		return base.PlayerManager.HeldInstance.Center + (vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f + -0.3125f * vector;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Action != ActionType.LowerToCornerLedge)
		{
			return false;
		}
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		float num = 4f * (float)((!base.LevelManager.Descending) ? 1 : (-1)) / base.CameraManager.PixelsPerTrixel;
		Vector3 vector2 = (vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f + num * Vector3.UnitY;
		Vector3 value = base.PlayerManager.HeldInstance.Center + vector2;
		if (!base.CameraManager.StickyCam && !base.CameraManager.Constrained && !base.CameraManager.PanningConstraints.HasValue)
		{
			base.CameraManager.Center = Vector3.Lerp(camOrigin, value, base.PlayerManager.Animation.Timing.NormalizedStep);
		}
		base.PlayerManager.Position = base.PlayerManager.HeldInstance.Center + (vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f;
		Vector3 vector3 = playerOrigin - base.PlayerManager.Position;
		base.PlayerManager.SplitUpCubeCollectorOffset = vector3 * (1f - base.PlayerManager.Animation.Timing.NormalizedStep);
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.LookingDirection = base.PlayerManager.LookingDirection.GetOpposite();
			base.PlayerManager.SplitUpCubeCollectorOffset = Vector3.Zero;
			base.PlayerManager.Action = ActionType.GrabCornerLedge;
		}
		base.PlayerManager.Animation.Timing.Update(elapsed, 1.25f);
		return false;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.LowerToCornerLedge;
	}
}
