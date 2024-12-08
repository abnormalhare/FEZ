using System.Collections.Generic;
using Common;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

internal class DisplayModeEqualityComparer : IEqualityComparer<DisplayMode>
{
	public static readonly DisplayModeEqualityComparer Default = new DisplayModeEqualityComparer();

	public bool Equals(DisplayMode x, DisplayMode y)
	{
		if (x.Width == y.Width)
		{
			return x.Height == y.Height;
		}
		return false;
	}

	public int GetHashCode(DisplayMode obj)
	{
		return Util.CombineHashCodes(obj.Width.GetHashCode(), obj.Height.GetHashCode());
	}
}
