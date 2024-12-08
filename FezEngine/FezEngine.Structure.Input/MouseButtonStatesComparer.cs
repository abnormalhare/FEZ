using System.Collections.Generic;

namespace FezEngine.Structure.Input;

public class MouseButtonStatesComparer : IEqualityComparer<MouseButtonStates>
{
	public static readonly MouseButtonStatesComparer Default = new MouseButtonStatesComparer();

	public bool Equals(MouseButtonStates x, MouseButtonStates y)
	{
		return x == y;
	}

	public int GetHashCode(MouseButtonStates obj)
	{
		return (int)obj;
	}
}
