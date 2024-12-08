using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public class Vector3SplineInterpolation : SplineInterpolation<Vector3>
{
	public Vector3SplineInterpolation(TimeSpan duration, params Vector3[] points)
		: base(duration, points)
	{
	}

	protected override void Interpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		if (SplineInterpolation<Vector3>.LongScreenshot)
		{
			base.Current = FezMath.Slerp(p1, p2, t);
		}
		else
		{
			base.Current = Vector3.CatmullRom(p0, p1, p2, p3, t);
		}
	}
}
