using System;
using System.Reflection;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedMatrixArray : SemanticMappedParameter<Matrix[]>, SemanticMappedArrayParameter<Matrix[]>
{
	private IntPtr raw;

	public SemanticMappedMatrixArray(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
		raw = (IntPtr)ReflectionHelper.GetValue(parameter.GetType().GetMember("values", BindingFlags.Instance | BindingFlags.NonPublic)[0], parameter);
	}

	protected override void DoSet(Matrix[] value)
	{
		Set(value, 0, value.Length);
	}

	protected override void DoSet(Matrix[] value, int length)
	{
		Set(value, 0, length);
	}

	public unsafe void Set(Matrix[] value, int start, int length)
	{
		int columnCount = parameter.ColumnCount;
		int rowCount = parameter.RowCount;
		float* ptr = (float*)(void*)raw;
		if (columnCount == 4 && rowCount == 4)
		{
			int num = start;
			while (num < start + length)
			{
				*ptr = value[num].M11;
				ptr[1] = value[num].M21;
				ptr[2] = value[num].M31;
				ptr[3] = value[num].M41;
				ptr[4] = value[num].M12;
				ptr[5] = value[num].M22;
				ptr[6] = value[num].M32;
				ptr[7] = value[num].M42;
				ptr[8] = value[num].M13;
				ptr[9] = value[num].M23;
				ptr[10] = value[num].M33;
				ptr[11] = value[num].M43;
				ptr[12] = value[num].M14;
				ptr[13] = value[num].M24;
				ptr[14] = value[num].M34;
				ptr[15] = value[num].M44;
				num++;
				ptr += 16;
			}
		}
		else if (columnCount == 3 && rowCount == 3)
		{
			int num2 = start;
			while (num2 < start + length)
			{
				*ptr = value[num2].M11;
				ptr[1] = value[num2].M21;
				ptr[2] = value[num2].M31;
				ptr[4] = value[num2].M12;
				ptr[5] = value[num2].M22;
				ptr[6] = value[num2].M32;
				ptr[8] = value[num2].M13;
				ptr[9] = value[num2].M23;
				ptr[10] = value[num2].M33;
				num2++;
				ptr += 12;
			}
		}
		else if (columnCount == 4 && rowCount == 3)
		{
			int num3 = start;
			while (num3 < start + length)
			{
				*ptr = value[num3].M11;
				ptr[1] = value[num3].M21;
				ptr[2] = value[num3].M31;
				ptr[3] = value[num3].M41;
				ptr[4] = value[num3].M12;
				ptr[5] = value[num3].M22;
				ptr[6] = value[num3].M32;
				ptr[7] = value[num3].M42;
				ptr[8] = value[num3].M13;
				ptr[9] = value[num3].M23;
				ptr[10] = value[num3].M33;
				ptr[11] = value[num3].M43;
				num3++;
				ptr += 12;
			}
		}
		else if (columnCount == 3 && rowCount == 4)
		{
			int num4 = start;
			while (num4 < start + length)
			{
				*ptr = value[num4].M11;
				ptr[1] = value[num4].M21;
				ptr[2] = value[num4].M31;
				ptr[4] = value[num4].M12;
				ptr[5] = value[num4].M22;
				ptr[6] = value[num4].M32;
				ptr[8] = value[num4].M13;
				ptr[9] = value[num4].M23;
				ptr[10] = value[num4].M33;
				ptr[12] = value[num4].M14;
				ptr[13] = value[num4].M24;
				ptr[14] = value[num4].M34;
				num4++;
				ptr += 16;
			}
		}
		else
		{
			if (columnCount != 2 || rowCount != 2)
			{
				throw new NotImplementedException("Matrix Size: " + rowCount + " " + columnCount);
			}
			int num5 = start;
			while (num5 < start + length)
			{
				*ptr = value[num5].M11;
				ptr[1] = value[num5].M21;
				ptr[4] = value[num5].M12;
				ptr[5] = value[num5].M22;
				num5++;
				ptr += 8;
			}
		}
	}
}
