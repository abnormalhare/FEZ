using System.Collections.Generic;
using FezEngine.Structure;

namespace FezEngine.Tools;

internal class ChunkedTrixelComparer : IComparer<TrixelEmplacement>
{
	public int Compare(TrixelEmplacement x, TrixelEmplacement y)
	{
		int num = x.X.CompareTo(y.X);
		if (num == 0)
		{
			num = x.Y.CompareTo(y.Y);
			if (num == 0)
			{
				num = x.Z.CompareTo(y.Z);
			}
		}
		return num;
	}
}
