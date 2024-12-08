using System.Collections.Generic;

namespace FezGame.Components;

internal class WarpDestinationsComparer : IEqualityComparer<WarpDestinations>
{
	public static readonly WarpDestinationsComparer Default = new WarpDestinationsComparer();

	public bool Equals(WarpDestinations x, WarpDestinations y)
	{
		return x == y;
	}

	public int GetHashCode(WarpDestinations obj)
	{
		return (int)obj;
	}
}
