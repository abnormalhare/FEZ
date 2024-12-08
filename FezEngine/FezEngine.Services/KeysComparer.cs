using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Services;

public class KeysComparer : IComparer<Keys>
{
	public static readonly KeysComparer Default = new KeysComparer();

	public int Compare(Keys x, Keys y)
	{
		if (x < y)
		{
			return -1;
		}
		if (x > y)
		{
			return 1;
		}
		return 0;
	}
}
