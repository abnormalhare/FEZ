using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public interface IPhysicsEntity
{
	MultipleHits<TrileInstance> Ground { get; set; }

	bool Grounded { get; }

	bool Sliding { get; }

	Vector3 Velocity { get; set; }

	Vector3 GroundMovement { get; set; }

	Vector3 Center { get; set; }

	Vector3 Size { get; }

	PointCollision[] CornerCollision { get; }

	MultipleHits<CollisionResult> WallCollision { get; set; }

	bool Background { get; set; }

	float Elasticity { get; }

	bool NoVelocityClamping { get; }
}
