using System.Collections.Generic;

namespace FezEngine.Structure.Input;

public class FezButtonStateComparer : IEqualityComparer<FezButtonState>
{
	public static readonly FezButtonStateComparer Default = new FezButtonStateComparer();

	public bool Equals(FezButtonState x, FezButtonState y)
	{
		return x == y;
	}

	public int GetHashCode(FezButtonState obj)
	{
		return (int)obj;
	}
}
