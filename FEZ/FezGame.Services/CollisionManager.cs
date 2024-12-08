using System;
using Common;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public class CollisionManager : ICollisionManager
{
	private float gravityFactor;

	public float DistanceEpsilon => 0.001f;

	public float GravityFactor
	{
		get
		{
			return gravityFactor;
		}
		set
		{
			if (value != gravityFactor)
			{
				gravityFactor = value;
				this.GravityChanged();
			}
		}
	}

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	public event Action GravityChanged = Util.NullAction;

	public CollisionManager()
	{
		gravityFactor = 1f;
	}

	public void CollideRectangle(Vector3 position, Vector3 impulse, Vector3 size, out MultipleHits<CollisionResult> horizontalResults, out MultipleHits<CollisionResult> verticalResults)
	{
		CollideRectangle(position, impulse, size, QueryOptions.None, out horizontalResults, out verticalResults);
	}

	public void CollideRectangle(Vector3 position, Vector3 impulse, Vector3 size, QueryOptions options, out MultipleHits<CollisionResult> horizontalResults, out MultipleHits<CollisionResult> verticalResults)
	{
		CollideRectangle(position, impulse, size, options, 0f, out horizontalResults, out verticalResults);
	}

	public void CollideRectangle(Vector3 position, Vector3 impulse, Vector3 size, QueryOptions options, float elasticity, out MultipleHits<CollisionResult> horizontalResults, out MultipleHits<CollisionResult> verticalResults)
	{
		CollideRectangle(position, impulse, size, options, elasticity, CameraManager.Viewpoint, out horizontalResults, out verticalResults);
	}

	public void CollideRectangle(Vector3 position, Vector3 impulse, Vector3 size, QueryOptions options, float elasticity, Viewpoint viewpoint, out MultipleHits<CollisionResult> horizontalResults, out MultipleHits<CollisionResult> verticalResults)
	{
		Vector3 vector = ((viewpoint == Viewpoint.Front || viewpoint == Viewpoint.Back) ? new Vector3(impulse.X, 0f, 0f) : new Vector3(0f, 0f, impulse.Z));
		Vector3 vector2 = new Vector3(0f, impulse.Y, 0f);
		Vector3 halfSize = size / 2f;
		horizontalResults = CollideEdge(position, vector, halfSize, Direction2D.Horizontal, options, elasticity, viewpoint);
		verticalResults = CollideEdge(position, vector2, halfSize, Direction2D.Vertical, options, elasticity, viewpoint);
		if ((options & QueryOptions.Simple) != QueryOptions.Simple && !horizontalResults.AnyCollided() && !verticalResults.AnyCollided())
		{
			horizontalResults = CollideEdge(position + vector2, vector, halfSize, Direction2D.Horizontal, options, elasticity, viewpoint);
			verticalResults = CollideEdge(position + vector, vector2, halfSize, Direction2D.Vertical, options, elasticity, viewpoint);
		}
	}

	public MultipleHits<CollisionResult> CollideEdge(Vector3 position, Vector3 impulse, Vector3 halfSize, Direction2D direction)
	{
		return CollideEdge(position, impulse, halfSize, direction, QueryOptions.None);
	}

	public MultipleHits<CollisionResult> CollideEdge(Vector3 position, Vector3 impulse, Vector3 halfSize, Direction2D direction, QueryOptions options)
	{
		return CollideEdge(position, impulse, halfSize, direction, options, 0f);
	}

	public MultipleHits<CollisionResult> CollideEdge(Vector3 position, Vector3 impulse, Vector3 halfSize, Direction2D direction, QueryOptions options, float elasticity)
	{
		return CollideEdge(position, impulse, halfSize, direction, options, elasticity, CameraManager.Viewpoint);
	}

	public MultipleHits<CollisionResult> CollideEdge(Vector3 position, Vector3 impulse, Vector3 halfSize, Direction2D direction, QueryOptions options, float elasticity, Viewpoint viewpoint)
	{
		MultipleHits<CollisionResult> result = default(MultipleHits<CollisionResult>);
		if (impulse == Vector3.Zero)
		{
			return result;
		}
		bool flag = (options & QueryOptions.Simple) == QueryOptions.Simple;
		Vector3 vector = new Vector3(Math.Sign(impulse.X), Math.Sign(impulse.Y), Math.Sign(impulse.Z));
		if (!flag)
		{
			Vector3 position2 = position;
			Vector3 position3 = position;
			switch (direction)
			{
			case Direction2D.Horizontal:
				position2 += (vector + Vector3.Down) * halfSize;
				position3 += (vector + Vector3.Up) * halfSize;
				break;
			case Direction2D.Vertical:
			{
				Vector3 vector2 = viewpoint.RightVector() * PlayerManager.LookingDirection.Sign();
				position2 += (vector - vector2) * halfSize;
				position3 += (vector + vector2) * halfSize;
				break;
			}
			}
			result.NearLow = CollidePoint(position2, impulse, options, elasticity, viewpoint);
			result.FarHigh = CollidePoint(position3, impulse, options, elasticity, viewpoint);
		}
		if (flag || !result.NearLow.Collided)
		{
			Vector3 position4 = position + vector * halfSize;
			result.NearLow = CollidePoint(position4, impulse, options, elasticity, viewpoint);
		}
		return result;
	}

	public CollisionResult CollidePoint(Vector3 position, Vector3 impulse)
	{
		return CollidePoint(position, impulse, QueryOptions.None);
	}

	public CollisionResult CollidePoint(Vector3 position, Vector3 impulse, QueryOptions options)
	{
		return CollidePoint(position, impulse, options, 0f);
	}

	public CollisionResult CollidePoint(Vector3 position, Vector3 impulse, QueryOptions options, float elasticity)
	{
		return CollidePoint(position, impulse, options, elasticity, CameraManager.Viewpoint);
	}

	public CollisionResult CollidePoint(Vector3 position, Vector3 impulse, QueryOptions options, float elasticity, Viewpoint viewpoint)
	{
		CollisionResult result = default(CollisionResult);
		Vector3 vector = position + impulse;
		TrileInstance trileInstance = null;
		if ((options & QueryOptions.Background) != 0)
		{
			trileInstance = LevelManager.ActualInstanceAt(vector);
		}
		if (trileInstance == null)
		{
			NearestTriles nearestTriles = LevelManager.NearestTrile(vector, options, viewpoint);
			trileInstance = nearestTriles.Deep ?? nearestTriles.Surface;
		}
		bool flag = GravityFactor < 0f;
		if (trileInstance != null)
		{
			result = CollideWithInstance(position, vector, impulse, trileInstance, options, elasticity, viewpoint, flag);
		}
		if ((options & QueryOptions.Background) != 0 && !result.Collided && impulse.Y < 0f)
		{
			NearestTriles nearestTriles2 = LevelManager.NearestTrile(vector, options, viewpoint);
			trileInstance = nearestTriles2.Deep ?? nearestTriles2.Surface;
			if (trileInstance != null)
			{
				result = CollideWithInstance(position, vector, impulse, trileInstance, options, elasticity, viewpoint, flag);
			}
		}
		if (result.Collided && (flag ? (impulse.Y > 0f) : (impulse.Y < 0f)))
		{
			if ((double)vector.X % 0.25 == 0.0)
			{
				vector.X += 0.001f;
			}
			if ((double)vector.Z % 0.25 == 0.0)
			{
				vector.Z += 0.001f;
			}
			TrileInstance trileInstance2 = LevelManager.ActualInstanceAt(vector);
			CollisionType rotatedFace;
			result.ShouldBeClamped = trileInstance2 == null || !trileInstance2.Enabled || (rotatedFace = trileInstance2.GetRotatedFace(CameraManager.VisibleOrientation)) == CollisionType.None || rotatedFace == CollisionType.Immaterial;
		}
		return result;
	}

	private static CollisionResult CollideWithInstance(Vector3 origin, Vector3 destination, Vector3 impulse, TrileInstance instance, QueryOptions options, float elasticity, Viewpoint viewpoint, bool invertedGravity)
	{
		CollisionResult result = default(CollisionResult);
		Vector3 normal = -impulse.Sign();
		FaceOrientation faceOrientation = viewpoint.VisibleOrientation();
		if ((options & QueryOptions.Background) == QueryOptions.Background)
		{
			faceOrientation = faceOrientation.GetOpposite();
		}
		CollisionType rotatedFace = instance.GetRotatedFace(faceOrientation);
		if (rotatedFace != CollisionType.None)
		{
			result.Destination = instance;
			result.NearestDistance = instance.Center;
			result.Response = SolidCollision(normal, instance, origin, destination, impulse, elasticity);
			if (result.Response != Vector3.Zero)
			{
				int collided;
				switch (rotatedFace)
				{
				case CollisionType.TopOnly:
				case CollisionType.TopNoStraightLedge:
					collided = ((invertedGravity ? (normal.Y < 0f) : (normal.Y > 0f)) ? 1 : 0);
					break;
				default:
					collided = 0;
					break;
				case CollisionType.AllSides:
					collided = 1;
					break;
				}
				result.Collided = (byte)collided != 0;
			}
		}
		return result;
	}

	private static Vector3 SolidCollision(Vector3 normal, TrileInstance instance, Vector3 origin, Vector3 destination, Vector3 impulse, float elasticity)
	{
		Vector3 vector = instance.TransformedSize / 2f;
		Vector3 vector2 = instance.Center + vector * normal;
		Vector3 vector3 = Vector3.Zero;
		Vector3 vector5;
		if (instance.PhysicsState != null)
		{
			Vector3 vector4 = (instance.PhysicsState.Sticky ? FezMath.XZMask : Vector3.One);
			vector5 = instance.Center - instance.PhysicsState.Velocity * vector4 + vector * normal;
			vector3 = vector2 - vector5;
		}
		else
		{
			vector5 = vector2;
		}
		Vector3 a = origin - vector5;
		Vector3 a2 = destination - vector2;
		if (FezMath.AlmostClamp(a.Dot(normal)) >= 0f && FezMath.AlmostClamp(a2.Dot(normal)) <= 0f)
		{
			Vector3 vector6 = normal.Abs();
			if (elasticity > 0f)
			{
				return (vector3 - impulse) * vector6 * (1f + elasticity);
			}
			return (vector2 - destination) * vector6;
		}
		return Vector3.Zero;
	}
}
