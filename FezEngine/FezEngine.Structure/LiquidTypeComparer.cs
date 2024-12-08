using System.Collections.Generic;

namespace FezEngine.Structure;

public class LiquidTypeComparer : IEqualityComparer<LiquidType>
{
	public static readonly LiquidTypeComparer Default = new LiquidTypeComparer();

	public bool Equals(LiquidType x, LiquidType y)
	{
		return x == y;
	}

	public int GetHashCode(LiquidType obj)
	{
		return (int)obj;
	}
}
