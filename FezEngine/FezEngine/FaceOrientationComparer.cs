using System.Collections.Generic;

namespace FezEngine;

public class FaceOrientationComparer : IEqualityComparer<FaceOrientation>
{
	public static readonly FaceOrientationComparer Default = new FaceOrientationComparer();

	public bool Equals(FaceOrientation x, FaceOrientation y)
	{
		return x == y;
	}

	public int GetHashCode(FaceOrientation obj)
	{
		return (int)obj;
	}
}
