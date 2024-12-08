using System.Collections.Generic;

namespace FezEngine;

public class HorizontalDirectionComparer : IEqualityComparer<HorizontalDirection>
{
	public static readonly HorizontalDirectionComparer Default = new HorizontalDirectionComparer();

	public bool Equals(HorizontalDirection x, HorizontalDirection y)
	{
		return x == y;
	}

	public int GetHashCode(HorizontalDirection obj)
	{
		return (int)obj;
	}
}
