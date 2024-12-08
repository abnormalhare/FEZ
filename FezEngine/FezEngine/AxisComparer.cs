using System.Collections.Generic;

namespace FezEngine;

public class AxisComparer : IEqualityComparer<Axis>
{
	public static readonly AxisComparer Default = new AxisComparer();

	public bool Equals(Axis x, Axis y)
	{
		return x == y;
	}

	public int GetHashCode(Axis obj)
	{
		return (int)obj;
	}
}
