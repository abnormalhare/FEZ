using FezEngine.Services;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public struct PointCollision
{
	public readonly Vector3 Point;

	public readonly NearestTriles Instances;

	public PointCollision(Vector3 point, NearestTriles instances)
	{
		Point = point;
		Instances = instances;
	}
}
