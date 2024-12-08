using System.Collections.Generic;

namespace FezEngine.Structure;

public class ShardNoteComparer : IEqualityComparer<ShardNotes>
{
	public static readonly ShardNoteComparer Default = new ShardNoteComparer();

	public bool Equals(ShardNotes x, ShardNotes y)
	{
		return x == y;
	}

	public int GetHashCode(ShardNotes obj)
	{
		return (int)obj;
	}
}
