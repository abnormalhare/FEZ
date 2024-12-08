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

public class GrabCornerLedge : PlayerAction
{
	private const float VelocityThreshold = 0.025f;

	private const float MovementThreshold = 0.1f;

	private const float DistanceThreshold = 0.35f;

	private Viewpoint? rotatedFrom;

	private Vector3 huggedCorner;

	private SoundEffect sound;

	protected override bool ViewTransitionIndependent => true;

	public GrabCornerLedge(Game game)
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
		if (action != ActionType.Jumping && action != ActionType.Falling)
		{
			return;
		}
		HorizontalDirection lookingDirection = base.PlayerManager.LookingDirection;
		if (lookingDirection == HorizontalDirection.None)
		{
			return;
		}
		Vector3 vector = base.CameraManager.Viewpoint.ScreenSpaceMask();
		Vector3 vector2 = base.CameraManager.Viewpoint.RightVector() * lookingDirection.Sign();
		float num = base.PlayerManager.Velocity.Dot(vector2);
		float num2 = base.InputManager.Movement.X * (float)lookingDirection.Sign();
		if (!(num > 0.025f) && !(num2 > 0.1f))
		{
			return;
		}
		MultipleHits<CollisionResult> wallCollision = base.PlayerManager.WallCollision;
		FaceOrientation visibleOrientation = base.CameraManager.VisibleOrientation;
		TrileInstance trileInstance = wallCollision.NearLow.Destination ?? base.PlayerManager.CornerCollision[1 + ((lookingDirection == HorizontalDirection.Left) ? 2 : 0)].Instances.Deep;
		TrileInstance trileInstance2 = wallCollision.FarHigh.Destination ?? base.PlayerManager.CornerCollision[(lookingDirection == HorizontalDirection.Left) ? 2 : 0].Instances.Deep;
		Trile trile = trileInstance?.Trile;
		if (trileInstance == null || trileInstance.GetRotatedFace(visibleOrientation) == CollisionType.None || trile.ActorSettings.Type == ActorType.Ladder || trileInstance == trileInstance2 || (trileInstance2 != null && trileInstance2.GetRotatedFace(visibleOrientation) == CollisionType.AllSides) || !trileInstance.Enabled)
		{
			return;
		}
		TrileInstance trileInstance3 = base.LevelManager.ActualInstanceAt(trileInstance.Center - vector2);
		TrileInstance deep = base.LevelManager.NearestTrile(trileInstance.Center - vector2).Deep;
		if ((deep != null && deep.Enabled && deep.GetRotatedFace(base.CameraManager.VisibleOrientation) != CollisionType.None) || (trileInstance3 != null && trileInstance3.Enabled && !trileInstance3.Trile.Immaterial) || (base.PlayerManager.Action == ActionType.Jumping && (double)((trileInstance.Center - base.PlayerManager.LeaveGroundPosition) * vector).Length() < 1.25) || (trileInstance.GetRotatedFace(visibleOrientation) != 0 && base.CollisionManager.CollideEdge(trileInstance.Center + trileInstance.TransformedSize * (Vector3.UnitY * 0.498f + vector2 * 0.5f), Vector3.Down * Math.Sign(base.CollisionManager.GravityFactor), base.PlayerManager.Size * FezMath.XZMask / 2f, Direction2D.Vertical).AnyHit()))
		{
			return;
		}
		Vector3 vector3 = (-vector2 + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * trileInstance.TransformedSize / 2f;
		Vector3 vector4 = base.PlayerManager.Center * vector;
		Vector3 vector5 = trileInstance.Center * vector;
		if (!((vector4 - (vector5 + vector3)).Length() < 0.35f))
		{
			return;
		}
		base.PlayerManager.HeldInstance = trileInstance;
		base.PlayerManager.Action = ActionType.GrabCornerLedge;
		Waiters.Wait(0.1, delegate
		{
			if (base.PlayerManager.HeldInstance != null)
			{
				sound.EmitAt(base.PlayerManager.Position);
				base.InputManager.ActiveGamepad.Vibrate(VibrationMotor.LeftLow, 0.10000000149011612, TimeSpan.FromSeconds(0.20000000298023224));
				base.InputManager.ActiveGamepad.Vibrate(VibrationMotor.RightHigh, 0.4000000059604645, TimeSpan.FromSeconds(0.20000000298023224));
			}
		});
	}

	protected override void Begin()
	{
		base.Begin();
		base.PlayerManager.Velocity = Vector3.Zero;
		Vector3 vector = base.CameraManager.Viewpoint.RightVector();
		base.PlayerManager.GroundedVelocity = vector * 0.085f + Vector3.UnitY * 0.15f * Math.Sign(base.CollisionManager.GravityFactor);
		base.InputManager.PressedToDown();
		base.GomezService.OnGrabLedge();
	}

	public override void Update(GameTime gameTime)
	{
		if (rotatedFrom.HasValue && (double)base.CameraManager.ViewTransitionStep >= 0.6)
		{
			int distance = base.CameraManager.Viewpoint.GetDistance(rotatedFrom.Value);
			if (Math.Abs(distance) % 2 == 0)
			{
				base.PlayerManager.LookingDirection = base.PlayerManager.LookingDirection.GetOpposite();
			}
			else
			{
				if (base.PlayerManager.LookingDirection == HorizontalDirection.Right)
				{
					base.PlayerManager.Action = ((Math.Sign(distance) > 0) ? ActionType.GrabLedgeBack : ActionType.GrabLedgeFront);
				}
				else
				{
					base.PlayerManager.Action = ((Math.Sign(distance) > 0) ? ActionType.GrabLedgeFront : ActionType.GrabLedgeBack);
				}
				if (base.PlayerManager.Action == ActionType.GrabLedgeBack)
				{
					base.PlayerManager.Position -= base.PlayerManager.Size.Z / 4f * base.CameraManager.Viewpoint.ForwardVector();
					CorrectWallOverlap(overcompensate: true);
					base.PlayerManager.Background = false;
				}
				else
				{
					base.PlayerManager.Position += base.PlayerManager.Size.Z / 4f * base.CameraManager.Viewpoint.ForwardVector();
					base.PlayerManager.Background = true;
				}
			}
			SyncAnimation(IsActionAllowed(base.PlayerManager.Action));
			rotatedFrom = null;
		}
		if (base.PlayerManager.Action.IsOnLedge())
		{
			if (base.PlayerManager.HeldInstance == null)
			{
				base.PlayerManager.Action = ActionType.Idle;
			}
			else if (base.PlayerManager.HeldInstance.PhysicsState != null && (double)Math.Abs(base.PlayerManager.HeldInstance.PhysicsState.Velocity.Dot(Vector3.One)) > 0.5)
			{
				base.PlayerManager.Velocity = base.PlayerManager.HeldInstance.PhysicsState.Velocity;
				base.PlayerManager.HeldInstance = null;
				base.PlayerManager.Action = ActionType.Jumping;
			}
		}
		base.Update(gameTime);
	}

	private void CorrectWallOverlap(bool overcompensate)
	{
		PointCollision[] cornerCollision = base.PlayerManager.CornerCollision;
		for (int i = 0; i < cornerCollision.Length; i++)
		{
			PointCollision pointCollision = cornerCollision[i];
			TrileInstance deep = pointCollision.Instances.Deep;
			if (deep != null && deep != base.PlayerManager.CarriedInstance && deep.GetRotatedFace(base.CameraManager.VisibleOrientation) == CollisionType.AllSides)
			{
				Vector3 vector = (pointCollision.Point - (deep.Center + (base.PlayerManager.Position - pointCollision.Point).Sign() * deep.TransformedSize / 2f)) * base.CameraManager.Viewpoint.SideMask();
				base.PlayerManager.Position -= vector;
				if (overcompensate)
				{
					base.PlayerManager.Position -= vector.Sign() * 0.001f * 2f;
				}
				if (base.PlayerManager.Velocity.Sign() == vector.Sign())
				{
					Vector3 vector2 = vector.Sign().Abs();
					base.PlayerManager.Position -= base.PlayerManager.Velocity * vector2;
					base.PlayerManager.Velocity *= Vector3.One - vector2;
				}
				break;
			}
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		NearestTriles nearestTriles = base.LevelManager.NearestTrile(base.PlayerManager.HeldInstance.Center);
		CollisionType collisionType = CollisionType.None;
		bool flag = false;
		if (nearestTriles.Deep != null)
		{
			collisionType = nearestTriles.Deep.GetRotatedFace(base.CameraManager.Viewpoint.VisibleOrientation());
			flag = flag || collisionType == CollisionType.AllSides;
		}
		if (flag && (base.InputManager.RotateLeft == FezButtonState.Pressed || base.InputManager.RotateRight == FezButtonState.Pressed))
		{
			base.InputManager.PressedToDown();
		}
		if (nearestTriles.Deep == null)
		{
			flag = true;
		}
		if (nearestTriles.Deep != null)
		{
			flag = flag || collisionType == CollisionType.TopNoStraightLedge;
		}
		FezButtonState fezButtonState = ((!base.PlayerManager.Animation.Timing.Ended) ? FezButtonState.Pressed : FezButtonState.Down);
		if (base.CameraManager.ActionRunning && !flag && ((base.PlayerManager.LookingDirection == HorizontalDirection.Right && base.InputManager.Right == fezButtonState) || (base.PlayerManager.LookingDirection == HorizontalDirection.Left && base.InputManager.Left == fezButtonState)))
		{
			bool background = base.PlayerManager.Background;
			Vector3 position = base.PlayerManager.Position;
			base.PlayerManager.Position += base.CameraManager.Viewpoint.RightVector() * -base.PlayerManager.LookingDirection.Sign() * 0.5f;
			base.PhysicsManager.DetermineInBackground(base.PlayerManager, allowEnterInBackground: true, viewpointChanged: false, keepInFront: false);
			bool background2 = base.PlayerManager.Background;
			base.PlayerManager.Background = background;
			base.PlayerManager.Position = position;
			if (!background2)
			{
				FaceOrientation face = base.CameraManager.Viewpoint.VisibleOrientation();
				TrileInstance deep = base.PlayerManager.AxisCollision[VerticalDirection.Up].Deep;
				TrileInstance deep2 = base.PlayerManager.AxisCollision[VerticalDirection.Down].Deep;
				if ((deep == null || deep.GetRotatedFace(face) != 0) && deep2 != null && deep2.GetRotatedFace(face) == CollisionType.TopOnly && !base.CollisionManager.CollideEdge(deep2.Center, Vector3.Down * Math.Sign(base.CollisionManager.GravityFactor), base.PlayerManager.Size * FezMath.XZMask / 2f, Direction2D.Vertical).AnyHit())
				{
					TrileInstance surface = base.PlayerManager.AxisCollision[VerticalDirection.Down].Surface;
					if ((surface == null || !surface.Trile.ActorSettings.Type.IsClimbable()) && deep2.Enabled)
					{
						base.PlayerManager.Action = ActionType.FromCornerBack;
					}
				}
			}
		}
		Vector3 vector = ((!rotatedFrom.HasValue) ? (base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign()) : (rotatedFrom.Value.RightVector() * base.PlayerManager.LookingDirection.Sign()));
		huggedCorner = (-vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f;
		base.PlayerManager.Position = base.PlayerManager.HeldInstance.Center + huggedCorner;
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.GrabCornerLedge;
	}
}
