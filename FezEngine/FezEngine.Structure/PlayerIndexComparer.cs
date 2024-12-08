using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class PlayerIndexComparer : IEqualityComparer<PlayerIndex>
{
	public static readonly PlayerIndexComparer Default = new PlayerIndexComparer();

	public bool Equals(PlayerIndex x, PlayerIndex y)
	{
		return x == y;
	}

	public int GetHashCode(PlayerIndex obj)
	{
		return (int)obj;
	}
}
