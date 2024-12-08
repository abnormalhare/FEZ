using System.Collections.Generic;

namespace FezGame.Structure;

public class ActionTypeComparer : IEqualityComparer<ActionType>
{
	public static readonly ActionTypeComparer Default = new ActionTypeComparer();

	public bool Equals(ActionType x, ActionType y)
	{
		return x == y;
	}

	public int GetHashCode(ActionType obj)
	{
		return (int)obj;
	}
}
