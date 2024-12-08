using System;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

public class VaryingVector3 : VaryingValue<Vector3>
{
	protected override Func<Vector3, Vector3, Vector3> DefaultFunction => (Vector3 b, Vector3 v) => (v == Vector3.Zero) ? b : (b + new Vector3(RandomHelper.Centered(v.X), RandomHelper.Centered(v.Y), RandomHelper.Centered(v.Z)));

	public static Func<Vector3, Vector3, Vector3> Uniform => delegate(Vector3 b, Vector3 v)
	{
		float num = RandomHelper.Centered(1.0);
		return new Vector3(b.X + num * v.X, b.Y + num * v.Y, b.Z + num * v.Z);
	};

	public static Func<Vector3, Vector3, Vector3> ClampToTrixels => (Vector3 b, Vector3 v) => ((b + new Vector3(RandomHelper.Centered(v.X), RandomHelper.Centered(v.Y), RandomHelper.Centered(v.Z))) * 16f).Round() / 16f;

	public static implicit operator VaryingVector3(Vector3 value)
	{
		return new VaryingVector3
		{
			Base = value
		};
	}
}
