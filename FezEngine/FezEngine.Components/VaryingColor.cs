using System;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

public class VaryingColor : VaryingValue<Color>
{
	protected override Func<Color, Color, Color> DefaultFunction => (Color b, Color v) => (v == new Color(0, 0, 0, 0)) ? b : new Color((float)(int)b.R / 255f + RandomHelper.Centered((float)(int)v.R / 255f), (float)(int)b.G / 255f + RandomHelper.Centered((float)(int)v.G / 255f), (float)(int)b.B / 255f + RandomHelper.Centered((float)(int)v.B / 255f), (float)(int)b.A / 255f + RandomHelper.Centered((float)(int)v.A / 255f));

	public static Func<Color, Color, Color> Uniform => delegate(Color b, Color v)
	{
		Vector4 vector = b.ToVector4();
		Vector4 vector2 = v.ToVector4();
		float num = RandomHelper.Centered(1.0);
		return new Color(new Vector4(vector.X + num * vector2.X, vector.Y + num * vector2.Y, vector.Z + num * vector2.Z, vector.W + num * vector2.W));
	};

	public static implicit operator VaryingColor(Color value)
	{
		return new VaryingColor
		{
			Base = value
		};
	}
}
