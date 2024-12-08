using System.Collections.Generic;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public class MovingTrileInstanceComparer : IComparer<TrileInstance>
{
	private Vector3 ordering;

	public MovingTrileInstanceComparer(Vector3 ordering)
	{
		this.ordering = ordering.Sign();
	}

	public int Compare(TrileInstance lhs, TrileInstance rhs)
	{
		Vector3 position = rhs.Position;
		Vector3 position2 = lhs.Position;
		int num = position.X.CompareTo(position2.X) * (int)ordering.X;
		if (num == 0)
		{
			num = position.Y.CompareTo(position2.Y) * (int)ordering.Y;
			if (num == 0)
			{
				num = position.Z.CompareTo(position2.Z) * (int)ordering.Z;
			}
		}
		return num;
	}
}
