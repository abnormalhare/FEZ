using System;
using FezEngine.Tools;

namespace FezEngine.Components;

public class VaryingSingle : VaryingValue<float>
{
	protected override Func<float, float, float> DefaultFunction => (float b, float v) => (v != 0f) ? (b + RandomHelper.Centered(v)) : b;

	public static implicit operator VaryingSingle(float value)
	{
		return new VaryingSingle
		{
			Base = value
		};
	}
}
