using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public class MovingPositionComparer : IComparer<Vector3>
{
	private Vector3 ordering;

	public MovingPositionComparer(Vector3 ordering)
	{
		this.ordering = ordering.Sign();
	}

	public int Compare(Vector3 lhs, Vector3 rhs)
	{
		int num = rhs.X.CompareTo(lhs.X) * (int)ordering.X;
		if (num == 0)
		{
			num = rhs.Y.CompareTo(lhs.Y) * (int)ordering.Y;
			if (num == 0)
			{
				num = rhs.Z.CompareTo(lhs.Z) * (int)ordering.Z;
			}
		}
		return num;
	}
}
