using System.Collections.Generic;

namespace FezEngine;

public class ViewpointComparer : IEqualityComparer<Viewpoint>
{
	public static readonly ViewpointComparer Default = new ViewpointComparer();

	public bool Equals(Viewpoint x, Viewpoint y)
	{
		return x == y;
	}

	public int GetHashCode(Viewpoint obj)
	{
		return (int)obj;
	}
}
