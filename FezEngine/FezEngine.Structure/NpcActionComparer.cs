using System.Collections.Generic;

namespace FezEngine.Structure;

internal class NpcActionComparer : IEqualityComparer<NpcAction>
{
	public static readonly NpcActionComparer Default = new NpcActionComparer();

	public bool Equals(NpcAction x, NpcAction y)
	{
		return x == y;
	}

	public int GetHashCode(NpcAction obj)
	{
		return (int)obj;
	}
}
