using System.Collections.Generic;

namespace FezEngine;

public class CollisionTypeComparer : IEqualityComparer<CollisionType>
{
	public static readonly CollisionTypeComparer Default = new CollisionTypeComparer();

	public bool Equals(CollisionType x, CollisionType y)
	{
		return x == y;
	}

	public int GetHashCode(CollisionType obj)
	{
		return (int)obj;
	}
}
