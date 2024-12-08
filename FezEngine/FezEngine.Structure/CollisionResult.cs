using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public struct CollisionResult
{
	public bool Collided;

	public bool ShouldBeClamped;

	public Vector3 Response;

	public Vector3 NearestDistance;

	public TrileInstance Destination;

	public override string ToString()
	{
		return string.Format("{{{0} @ {1}}}", Collided, (Destination == null) ? "none" : Destination.ToString());
	}
}
