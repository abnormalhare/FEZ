using System;
using System.Reflection;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedVectorArray : SemanticMappedParameter<Vector4[]>, SemanticMappedArrayParameter<Vector4[]>
{
	private IntPtr raw;

	public SemanticMappedVectorArray(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
		raw = (IntPtr)ReflectionHelper.GetValue(parameter.GetType().GetMember("values", BindingFlags.Instance | BindingFlags.NonPublic)[0], parameter);
	}

	protected override void DoSet(Vector4[] value)
	{
		Set(value, 0, value.Length);
	}

	protected override void DoSet(Vector4[] value, int length)
	{
		Set(value, 0, length);
	}

	public unsafe void Set(Vector4[] value, int start, int length)
	{
		float* ptr = (float*)(void*)raw;
		int num = start;
		while (num < start + length)
		{
			*ptr = value[num].X;
			ptr[1] = value[num].Y;
			ptr[2] = value[num].Z;
			ptr[3] = value[num].W;
			num++;
			ptr += 4;
		}
	}
}
