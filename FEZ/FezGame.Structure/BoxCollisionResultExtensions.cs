using FezEngine.Structure;

namespace FezGame.Structure;

public static class BoxCollisionResultExtensions
{
	public static CollisionResult First(this MultipleHits<CollisionResult> result)
	{
		if (!result.NearLow.Collided)
		{
			if (!result.FarHigh.Collided)
			{
				return default(CollisionResult);
			}
			return result.FarHigh;
		}
		return result.NearLow;
	}

	public static bool AnyCollided(this MultipleHits<CollisionResult> result)
	{
		if (!result.NearLow.Collided)
		{
			return result.FarHigh.Collided;
		}
		return true;
	}

	public static bool AnyHit(this MultipleHits<CollisionResult> result)
	{
		if (result.NearLow.Destination == null)
		{
			return result.FarHigh.Destination != null;
		}
		return true;
	}
}
