using System.Collections.Generic;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Structure;

public interface IComplexPhysicsEntity : IPhysicsEntity
{
	bool MustBeClampedToGround { get; set; }

	Vector3? GroundedVelocity { get; set; }

	HorizontalDirection MovingDirection { get; set; }

	bool Climbing { get; }

	bool Swimming { get; }

	Dictionary<VerticalDirection, NearestTriles> AxisCollision { get; }

	MultipleHits<CollisionResult> Ceiling { get; set; }

	bool HandlesZClamping { get; }
}
