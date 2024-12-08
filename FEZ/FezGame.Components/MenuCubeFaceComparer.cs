using System.Collections.Generic;

namespace FezGame.Components;

internal class MenuCubeFaceComparer : IEqualityComparer<MenuCubeFace>
{
	public static readonly MenuCubeFaceComparer Default = new MenuCubeFaceComparer();

	public bool Equals(MenuCubeFace x, MenuCubeFace y)
	{
		return x == y;
	}

	public int GetHashCode(MenuCubeFace obj)
	{
		return (int)obj;
	}
}
