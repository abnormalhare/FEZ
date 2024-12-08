using System.Collections.Generic;

namespace FezEngine.Structure;

public class SurfaceTypeComparer : IEqualityComparer<SurfaceType>
{
	public static readonly SurfaceTypeComparer Default = new SurfaceTypeComparer();

	public bool Equals(SurfaceType x, SurfaceType y)
	{
		return x == y;
	}

	public int GetHashCode(SurfaceType obj)
	{
		return (int)obj;
	}
}
