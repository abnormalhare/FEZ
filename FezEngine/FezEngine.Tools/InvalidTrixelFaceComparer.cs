using System.Collections.Generic;
using FezEngine.Structure;

namespace FezEngine.Tools;

public class InvalidTrixelFaceComparer : IComparer<TrixelFace>
{
	public int Compare(TrixelFace x, TrixelFace y)
	{
		if (x.Face != y.Face)
		{
			return x.Face.CompareTo(y.Face);
		}
		switch (x.Face)
		{
		case FaceOrientation.Back:
		case FaceOrientation.Front:
		{
			int num = x.Id.Y.CompareTo(y.Id.Y);
			if (num != 0)
			{
				return num;
			}
			return x.Id.X.CompareTo(y.Id.X);
		}
		case FaceOrientation.Down:
		case FaceOrientation.Top:
		{
			int num = x.Id.X.CompareTo(y.Id.X);
			if (num != 0)
			{
				return num;
			}
			return x.Id.Z.CompareTo(y.Id.Z);
		}
		default:
		{
			int num = x.Id.Z.CompareTo(y.Id.Z);
			if (num != 0)
			{
				return num;
			}
			return x.Id.Y.CompareTo(y.Id.Y);
		}
		}
	}
}
