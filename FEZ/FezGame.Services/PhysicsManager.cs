using System;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public class PhysicsManager : IPhysicsManager
{
	public struct WallHuggingResult
	{
		public bool Hugged;

		public bool Behind;
	}

	public const float TrileSize = 0.15f;

	private const float GroundFriction = 0.85f;

	private const float WaterFriction = 0.925f;

	private const float SlidingFriction = 0.8f;

	private const float AirFriction = 0.9975f;

	private const float FallingSpeedLimit = 0.4f;

	private const float HuggingDistance = 0.002f;

	private static readonly Vector3 GroundFrictionV = new Vector3(0.85f, 1f, 0.85f);

	private static readonly Vector3 AirFrictionV = new Vector3(0.9975f, 1f, 0.9975f);

	private static readonly Vector3 WaterFrictionV = new Vector3(0.925f, 1f, 0.925f);

	private static readonly Vector3 SlidingFrictionV = new Vector3(0.8f, 1f, 0.8f);

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	public void DetermineOverlaps(IComplexPhysicsEntity entity)
	{
		DetermineOverlapsInternal(entity, CameraManager.Viewpoint);
		Vector3 center = entity.Center;
		Vector3 vector = entity.Size / 2f - new Vector3(0.001f);
		if (CollisionManager.GravityFactor < 0f)
		{
			vector *= new Vector3(1f, -1f, 1f);
		}
		QueryOptions options = (entity.Background ? QueryOptions.Background : QueryOptions.None);
		entity.AxisCollision[VerticalDirection.Up] = LevelManager.NearestTrile(center + vector * Vector3.Up, options);
		entity.AxisCollision[VerticalDirection.Down] = LevelManager.NearestTrile(center + vector * Vector3.Down, options);
	}

	public void DetermineOverlaps(ISimplePhysicsEntity entity)
	{
		DetermineOverlapsInternal(entity, CameraManager.Viewpoint);
	}

	private void DetermineOverlapsInternal(IPhysicsEntity entity, Viewpoint viewpoint)
	{
		QueryOptions queryOptions = QueryOptions.None;
		if (entity.Background)
		{
			queryOptions |= QueryOptions.Background;
		}
		Vector3 center = entity.Center;
		if (entity.CornerCollision.Length == 1)
		{
			entity.CornerCollision[0] = new PointCollision(center, LevelManager.NearestTrile(center, queryOptions, viewpoint));
			return;
		}
		Vector3 vector = viewpoint.RightVector();
		Vector3 vector2 = entity.Size / 2f - new Vector3(0.001f);
		if (CollisionManager.GravityFactor < 0f)
		{
			vector2 *= new Vector3(1f, -1f, 1f);
		}
		Vector3 vector3 = center + (vector + Vector3.Up) * vector2;
		entity.CornerCollision[0] = new PointCollision(vector3, LevelManager.NearestTrile(vector3, queryOptions, viewpoint));
		if (entity.CornerCollision[0].Instances.Deep == null && PlayerManager.CarriedInstance != null)
		{
			PlayerManager.CarriedInstance.PhysicsState.UpdatingPhysics = true;
			entity.CornerCollision[0] = new PointCollision(center, LevelManager.NearestTrile(center, queryOptions, viewpoint));
			PlayerManager.CarriedInstance.PhysicsState.UpdatingPhysics = false;
		}
		vector3 = center + (vector + Vector3.Down) * vector2;
		entity.CornerCollision[1] = new PointCollision(vector3, LevelManager.NearestTrile(vector3, queryOptions, viewpoint));
		vector3 = center + (-vector + Vector3.Up) * vector2;
		entity.CornerCollision[2] = new PointCollision(vector3, LevelManager.NearestTrile(vector3, queryOptions, viewpoint));
		vector3 = center + (-vector + Vector3.Down) * vector2;
		entity.CornerCollision[3] = new PointCollision(vector3, LevelManager.NearestTrile(vector3, queryOptions, viewpoint));
	}

	public bool DetermineInBackground(IPhysicsEntity entity, bool allowEnterInBackground, bool postRotation, bool keepInFront)
	{
		bool result = false;
		if (allowEnterInBackground)
		{
			Vector3? distance = null;
			Vector3 center = entity.Center;
			if (entity is IComplexPhysicsEntity)
			{
				IComplexPhysicsEntity obj = entity as IComplexPhysicsEntity;
				Vector3 impulse = 1f / 32f * Vector3.Down;
				QueryOptions queryOptions = QueryOptions.None;
				if (obj.Background)
				{
					queryOptions |= QueryOptions.Background;
				}
				bool flag = CollisionManager.CollideEdge(entity.Center, impulse, entity.Size / 2f, Direction2D.Vertical, queryOptions).AnyHit();
				if (!flag)
				{
					flag |= CollisionManager.CollideEdge(entity.Center, impulse, entity.Size / 2f, Direction2D.Vertical, queryOptions, 0f, CameraManager.Viewpoint.GetOpposite()).AnyHit();
				}
				if (obj.Grounded && !flag)
				{
					DebuggingBag.Add("zz. had to re-clamp to ground", "POSITIF");
					MultipleHits<CollisionResult> result2 = CollisionManager.CollideEdge(entity.Center, impulse, entity.Size / 2f, Direction2D.Vertical, queryOptions, 0f, CameraManager.LastViewpoint);
					if (result2.AnyCollided())
					{
						distance = result2.First().NearestDistance;
					}
				}
			}
			entity.Background = false;
			WallHuggingResult wallHuggingResult;
			do
			{
				DetermineOverlapsInternal(entity, CameraManager.Viewpoint);
				wallHuggingResult = HugWalls(entity, determineBackground: true, postRotation, keepInFront);
			}
			while (wallHuggingResult.Hugged);
			entity.Background = wallHuggingResult.Behind;
			if (!entity.Background && distance.HasValue)
			{
				result = true;
				entity.Center = center;
				ClampToGround(entity, distance, CameraManager.LastViewpoint);
				ClampToGround(entity, distance, CameraManager.Viewpoint);
				entity.Velocity *= Vector3.UnitY;
				entity.Background = false;
				do
				{
					DetermineOverlapsInternal(entity, CameraManager.Viewpoint);
					wallHuggingResult = HugWalls(entity, determineBackground: true, postRotation, keepInFront);
				}
				while (wallHuggingResult.Hugged);
				entity.Background = wallHuggingResult.Behind;
			}
			DetermineOverlapsInternal(entity, CameraManager.Viewpoint);
			HugWalls(entity, determineBackground: false, postRotation: false, keepInFront);
		}
		else if (entity.Background)
		{
			bool flag2 = true;
			PointCollision[] cornerCollision = entity.CornerCollision;
			for (int i = 0; i < cornerCollision.Length; i++)
			{
				PointCollision pointCollision = cornerCollision[i];
				flag2 &= !IsHuggable(pointCollision.Instances.Deep, entity);
			}
			if (flag2)
			{
				entity.Background = false;
			}
		}
		return result;
	}

	public WallHuggingResult HugWalls(IPhysicsEntity entity, bool determineBackground, bool postRotation, bool keepInFront)
	{
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		if (!entity.Background)
		{
			vector = -vector;
		}
		float num = 0.002f;
		if (entity is ISimplePhysicsEntity)
		{
			num = 0.0625f;
		}
		WallHuggingResult result = default(WallHuggingResult);
		Vector3 vector2 = default(Vector3);
		if (entity.Background && entity.Grounded)
		{
			return result;
		}
		PointCollision[] cornerCollision = entity.CornerCollision;
		for (int i = 0; i < cornerCollision.Length; i++)
		{
			PointCollision pointCollision = cornerCollision[i];
			TrileInstance trileInstance = null;
			if (IsHuggable(pointCollision.Instances.Surface, entity))
			{
				FaceOrientation face = FaceOrientation.Down;
				TrileEmplacement id = pointCollision.Instances.Surface.Emplacement.GetTraversal(ref face);
				TrileInstance trileInstance2 = LevelManager.TrileInstanceAt(ref id);
				if (trileInstance2 != null && trileInstance2.Enabled && trileInstance2.GetRotatedFace(CameraManager.VisibleOrientation) != CollisionType.None)
				{
					trileInstance = pointCollision.Instances.Surface;
				}
			}
			if (trileInstance == null && IsHuggable(pointCollision.Instances.Deep, entity))
			{
				trileInstance = pointCollision.Instances.Deep;
			}
			if (trileInstance != null && (!(entity is ISimplePhysicsEntity) || trileInstance.PhysicsState == null || !trileInstance.PhysicsState.Puppet) && trileInstance.PhysicsState != entity)
			{
				Vector3 vector3 = trileInstance.Center + vector * trileInstance.TransformedSize / 2f;
				Vector3 vector4 = entity.Center - vector * entity.Size / 2f - vector3 + num * -vector;
				float num2 = Vector3.Dot(vector4, vector);
				if (FezMath.AlmostClamp(num2) < 0f)
				{
					if (determineBackground && (!trileInstance.Trile.Thin || trileInstance.Trile.ForceHugging))
					{
						float num3 = Math.Abs((trileInstance.TransformedSize / 2f + entity.Size / 2f).Dot(vector));
						result.Behind |= Math.Abs(num2) > num3;
					}
					else if (keepInFront)
					{
						Vector3 vector5 = vector4 * vector.Abs();
						vector2 -= vector5;
						entity.Center -= vector5;
						result.Hugged = true;
					}
				}
			}
			if (!postRotation)
			{
				continue;
			}
			Vector3 vector6 = CameraManager.LastViewpoint.VisibleOrientation().AsVector();
			Vector3 position = pointCollision.Point + vector2;
			trileInstance = LevelManager.ActualInstanceAt(position);
			if (IsHuggable(trileInstance, entity))
			{
				Vector3 vector7 = trileInstance.Center + vector6 * trileInstance.TransformedSize.ZYX() / 2f;
				Vector3 vector8 = entity.Center - vector6 * entity.Size / 2f - vector7 + 0.002f * vector6;
				float num4 = Vector3.Dot(vector8, vector6);
				if (FezMath.AlmostClamp(num4) < 0f && num4 > -1f && keepInFront)
				{
					Vector3 vector9 = vector8 * vector6.Abs();
					vector2 -= vector9;
					entity.Center -= vector9;
					result.Hugged = true;
				}
			}
		}
		return result;
	}

	private static bool IsInstanceStateful(TrileInstance instance)
	{
		if (instance != null)
		{
			return instance.PhysicsState != null;
		}
		return false;
	}

	private bool IsHuggable(TrileInstance instance, IPhysicsEntity entity)
	{
		if (instance != null && instance.Enabled && !instance.Trile.Immaterial && (!instance.Trile.Thin || instance.Trile.ForceHugging) && instance != PlayerManager.CarriedInstance && instance != PlayerManager.PushedInstance && (!instance.Trile.ActorSettings.Type.IsBomb() || entity.Background) && (instance.PhysicsState == null || instance.PhysicsState != entity))
		{
			return !FezMath.In(instance.GetRotatedFace(entity.Background ? CameraManager.VisibleOrientation.GetOpposite() : CameraManager.VisibleOrientation), CollisionType.Immaterial, CollisionType.TopNoStraightLedge, CollisionType.AllSides, CollisionTypeComparer.Default);
		}
		return false;
	}

	public bool Update(IComplexPhysicsEntity entity)
	{
		QueryOptions queryOptions = QueryOptions.None;
		if (entity.Background)
		{
			queryOptions |= QueryOptions.Background;
		}
		MoveAlongWithGround(entity, queryOptions);
		CollisionManager.CollideRectangle(entity.Center, entity.Velocity, entity.Size, queryOptions, entity.Elasticity, out var horizontalResults, out var verticalResults);
		bool grounded = entity.Grounded;
		_ = entity.Ground;
		Vector3? clampToGroundDistance = null;
		FaceOrientation visibleOrientation = CameraManager.VisibleOrientation;
		bool flag = CollisionManager.GravityFactor < 0f;
		if (verticalResults.AnyCollided() && (flag ? (entity.Velocity.Y > 0f) : (entity.Velocity.Y < 0f)))
		{
			MultipleHits<TrileInstance> ground = entity.Ground;
			CollisionResult nearLow = verticalResults.NearLow;
			CollisionResult farHigh = verticalResults.FarHigh;
			if (farHigh.Destination != null && farHigh.Destination.GetRotatedFace(visibleOrientation) != CollisionType.None)
			{
				ground.FarHigh = farHigh.Destination;
				if (farHigh.Collided && (farHigh.ShouldBeClamped || entity.MustBeClampedToGround))
				{
					clampToGroundDistance = farHigh.NearestDistance;
				}
			}
			else
			{
				ground.FarHigh = null;
			}
			if (nearLow.Destination != null && nearLow.Destination.GetRotatedFace(visibleOrientation) != CollisionType.None)
			{
				ground.NearLow = nearLow.Destination;
				if (nearLow.Collided && (nearLow.ShouldBeClamped || entity.MustBeClampedToGround))
				{
					clampToGroundDistance = nearLow.NearestDistance;
				}
			}
			else
			{
				ground.NearLow = null;
			}
			entity.Ground = ground;
		}
		else
		{
			entity.Ground = default(MultipleHits<TrileInstance>);
		}
		if (entity.Velocity.Y > 0f && verticalResults.AnyCollided())
		{
			entity.Ceiling = verticalResults;
		}
		else
		{
			entity.Ceiling = default(MultipleHits<CollisionResult>);
		}
		bool flag2 = PlayerManager.Action == ActionType.Grabbing || PlayerManager.Action == ActionType.Pushing || PlayerManager.Action == ActionType.GrabCornerLedge || PlayerManager.Action == ActionType.LowerToCornerLedge || PlayerManager.Action == ActionType.SuckedIn || PlayerManager.Action == ActionType.Landing;
		flag2 |= entity.MustBeClampedToGround;
		entity.MustBeClampedToGround = false;
		flag2 |= entity.Grounded && entity.Ground.First.ForceClampToGround;
		if (grounded && !entity.Grounded)
		{
			entity.GroundedVelocity = entity.Velocity;
		}
		else if (!grounded && entity.Grounded)
		{
			entity.GroundedVelocity = null;
		}
		Vector3 vector = CameraManager.Viewpoint.RightVector();
		entity.MovingDirection = FezMath.DirectionFromMovement(Vector3.Dot(entity.Velocity, vector));
		bool flag3 = PlayerManager.Action == ActionType.FrontClimbingLadder || PlayerManager.Action == ActionType.FrontClimbingVine;
		if (entity.GroundMovement != Vector3.Zero || flag3)
		{
			DetermineInBackground(entity, allowEnterInBackground: true, postRotation: false, !PlayerManager.Climbing);
		}
		return UpdateInternal(entity, horizontalResults, verticalResults, clampToGroundDistance, grounded, !entity.HandlesZClamping, flag2, simple: false);
	}

	public bool Update(ISimplePhysicsEntity entity)
	{
		return Update(entity, simple: false, keepInFront: true);
	}

	public bool Update(ISimplePhysicsEntity entity, bool simple, bool keepInFront)
	{
		QueryOptions queryOptions = QueryOptions.None;
		if (entity.Background)
		{
			queryOptions |= QueryOptions.Background;
		}
		if (simple)
		{
			queryOptions |= QueryOptions.Simple;
		}
		if (entity is InstancePhysicsState)
		{
			(entity as InstancePhysicsState).UpdatingPhysics = true;
		}
		if (!simple)
		{
			MoveAlongWithGround(entity, queryOptions);
		}
		Vector3? clampToGroundDistance = null;
		bool grounded = entity.Grounded;
		MultipleHits<CollisionResult> horizontalResults;
		MultipleHits<CollisionResult> verticalResults;
		if (!entity.IgnoreCollision)
		{
			CollisionManager.CollideRectangle(entity.Center, entity.Velocity, entity.Size, queryOptions, entity.Elasticity, out horizontalResults, out verticalResults);
			bool num = CollisionManager.GravityFactor < 0f;
			FaceOrientation faceOrientation = CameraManager.VisibleOrientation;
			if (entity.Background)
			{
				faceOrientation = faceOrientation.GetOpposite();
			}
			if ((num ? (entity.Velocity.Y > 0f) : (entity.Velocity.Y < 0f)) && verticalResults.AnyCollided())
			{
				MultipleHits<TrileInstance> ground = entity.Ground;
				CollisionResult nearLow = verticalResults.NearLow;
				CollisionResult farHigh = verticalResults.FarHigh;
				if (farHigh.Destination != null && farHigh.Destination.GetRotatedFace(faceOrientation) != CollisionType.None)
				{
					ground.FarHigh = farHigh.Destination;
					if (farHigh.Collided && farHigh.ShouldBeClamped)
					{
						clampToGroundDistance = farHigh.NearestDistance;
					}
				}
				else
				{
					ground.FarHigh = null;
				}
				if (nearLow.Destination != null && nearLow.Destination.GetRotatedFace(faceOrientation) != CollisionType.None)
				{
					ground.NearLow = nearLow.Destination;
					if (nearLow.Collided && nearLow.ShouldBeClamped)
					{
						clampToGroundDistance = nearLow.NearestDistance;
					}
				}
				else
				{
					ground.NearLow = null;
				}
				entity.Ground = ground;
			}
			else
			{
				entity.Ground = default(MultipleHits<TrileInstance>);
			}
		}
		else
		{
			horizontalResults = default(MultipleHits<CollisionResult>);
			verticalResults = default(MultipleHits<CollisionResult>);
		}
		bool result = UpdateInternal(entity, horizontalResults, verticalResults, clampToGroundDistance, grounded, keepInFront, velocityIrrelevant: false, simple);
		if (entity is InstancePhysicsState)
		{
			(entity as InstancePhysicsState).UpdatingPhysics = false;
		}
		return result;
	}

	private bool UpdateInternal(IPhysicsEntity entity, MultipleHits<CollisionResult> horizontalResults, MultipleHits<CollisionResult> verticalResults, Vector3? clampToGroundDistance, bool wasGrounded, bool hugWalls, bool velocityIrrelevant, bool simple)
	{
		Vector3 velocity = entity.Velocity;
		if (!simple)
		{
			MultipleHits<CollisionResult> wallCollision = default(MultipleHits<CollisionResult>);
			if (horizontalResults.AnyCollided())
			{
				if (horizontalResults.NearLow.Collided)
				{
					wallCollision.NearLow = horizontalResults.NearLow;
				}
				if (horizontalResults.FarHigh.Collided)
				{
					wallCollision.FarHigh = horizontalResults.FarHigh;
				}
			}
			entity.WallCollision = wallCollision;
		}
		if (horizontalResults.NearLow.Collided)
		{
			velocity += horizontalResults.NearLow.Response;
		}
		else if (horizontalResults.FarHigh.Collided)
		{
			velocity += horizontalResults.FarHigh.Response;
		}
		if (verticalResults.NearLow.Collided)
		{
			velocity += verticalResults.NearLow.Response;
		}
		else if (verticalResults.FarHigh.Collided)
		{
			velocity += verticalResults.FarHigh.Response;
		}
		Vector3 value = ((entity is IComplexPhysicsEntity && (entity as IComplexPhysicsEntity).Swimming) ? WaterFrictionV : ((!(entity.Grounded || wasGrounded)) ? AirFrictionV : (entity.Sliding ? SlidingFrictionV : GroundFrictionV)));
		float amount = (1.2f + Math.Abs(CollisionManager.GravityFactor) * 0.8f) / 2f;
		velocity *= Vector3.Lerp(Vector3.One, value, amount);
		velocity = FezMath.AlmostClamp(velocity, 1E-06f);
		if (!entity.NoVelocityClamping)
		{
			float num = ((entity is IComplexPhysicsEntity) ? 0.4f : 0.38f);
			velocity.Y = MathHelper.Clamp(velocity.Y, 0f - num, num);
		}
		Vector3 center = entity.Center;
		Vector3 vector = center + velocity;
		bool flag = !FezMath.AlmostEqual(vector, center);
		entity.Velocity = velocity;
		if (flag)
		{
			entity.Center = vector;
		}
		if (velocityIrrelevant || flag)
		{
			DetermineInBackground(entity, allowEnterInBackground: false, postRotation: false, hugWalls);
			ClampToGround(entity, clampToGroundDistance, CameraManager.Viewpoint);
			if (hugWalls)
			{
				HugWalls(entity, determineBackground: false, postRotation: false, keepInFront: true);
			}
		}
		if (!simple && (!(entity is ISimplePhysicsEntity) || !(entity as ISimplePhysicsEntity).IgnoreCollision))
		{
			if (LevelManager.IsInvalidatingScreen)
			{
				ScheduleRedefineCorners(entity);
			}
			else
			{
				RedefineCorners(entity);
			}
		}
		return flag;
	}

	private void ScheduleRedefineCorners(IPhysicsEntity entity)
	{
		LevelManager.ScreenInvalidated += delegate
		{
			RedefineCorners(entity);
		};
	}

	private void RedefineCorners(IPhysicsEntity entity)
	{
		if (entity is IComplexPhysicsEntity)
		{
			DetermineOverlaps(entity as IComplexPhysicsEntity);
		}
		else
		{
			DetermineOverlaps(entity as ISimplePhysicsEntity);
		}
	}

	private void MoveAlongWithGround(IPhysicsEntity entity, QueryOptions queryOptions)
	{
		TrileInstance trileInstance = null;
		bool flag = false;
		if (IsInstanceStateful(entity.Ground.NearLow))
		{
			trileInstance = entity.Ground.NearLow;
		}
		else if (IsInstanceStateful(entity.Ground.FarHigh))
		{
			trileInstance = entity.Ground.FarHigh;
		}
		Vector3 vector = entity.GroundMovement;
		if (trileInstance != null)
		{
			entity.GroundMovement = FezMath.AlmostClamp(trileInstance.PhysicsState.Velocity + trileInstance.PhysicsState.GroundMovement);
			if (trileInstance.PhysicsState.Sticky)
			{
				flag = true;
			}
		}
		else
		{
			vector = Vector3.Clamp(vector, -FezMath.XZMask, Vector3.One);
			entity.GroundMovement = Vector3.Zero;
		}
		if (entity.GroundMovement != Vector3.Zero)
		{
			CollisionManager.CollideRectangle(entity.Center, entity.GroundMovement, entity.Size, queryOptions, entity.Elasticity, out var horizontalResults, out var verticalResults);
			entity.GroundMovement += (horizontalResults.AnyCollided() ? horizontalResults.First().Response : Vector3.Zero) + (verticalResults.AnyCollided() ? verticalResults.First().Response : Vector3.Zero);
			Vector3 min = (flag ? (-Vector3.One) : (-FezMath.XZMask));
			entity.Center += Vector3.Clamp(entity.GroundMovement, min, Vector3.One);
			if (vector == Vector3.Zero && entity.Velocity.Y > 0f)
			{
				entity.Velocity -= entity.GroundMovement * Vector3.UnitY;
			}
		}
		else if (!flag && vector != Vector3.Zero)
		{
			entity.Velocity += vector * 0.85f;
		}
	}

	public void ClampToGround(IPhysicsEntity entity, Vector3? distance, Viewpoint viewpoint)
	{
		if (distance.HasValue)
		{
			Vector3 mask = viewpoint.VisibleAxis().GetMask();
			entity.Center = distance.Value * mask + (Vector3.One - mask) * entity.Center;
		}
	}
}
