using System.Collections.Generic;

namespace FezEngine;

public class VerticalDirectionComparer : IEqualityComparer<VerticalDirection>
{
	public static readonly VerticalDirectionComparer Default = new VerticalDirectionComparer();

	public bool Equals(VerticalDirection x, VerticalDirection y)
	{
		return x == y;
	}

	public int GetHashCode(VerticalDirection obj)
	{
		return (int)obj;
	}
}
