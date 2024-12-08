using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Services;

public class KeysEqualityComparer : IEqualityComparer<Keys>
{
	public static readonly KeysEqualityComparer Default = new KeysEqualityComparer();

	public bool Equals(Keys x, Keys y)
	{
		return x == y;
	}

	public int GetHashCode(Keys obj)
	{
		return (int)obj;
	}
}
