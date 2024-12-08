using FezEngine.Structure.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Readers;

public class ShaderInstancedIndexedPrimitivesReader<TemplateType, InstanceType> : ContentTypeReader<ShaderInstancedIndexedPrimitives<TemplateType, InstanceType>> where TemplateType : struct, IShaderInstantiatableVertex where InstanceType : struct
{
	protected override ShaderInstancedIndexedPrimitives<TemplateType, InstanceType> Read(ContentReader input, ShaderInstancedIndexedPrimitives<TemplateType, InstanceType> existingInstance)
	{
		PrimitiveType primitiveType = input.ReadObject<PrimitiveType>();
		if (existingInstance == null)
		{
			existingInstance = new ShaderInstancedIndexedPrimitives<TemplateType, InstanceType>(primitiveType, (typeof(InstanceType) == typeof(Matrix)) ? 60 : 200, typeof(InstanceType) == typeof(Vector4));
		}
		else if (existingInstance.PrimitiveType != primitiveType)
		{
			existingInstance.PrimitiveType = primitiveType;
		}
		existingInstance.Vertices = input.ReadObject(existingInstance.Vertices);
		ushort[] array = input.ReadObject<ushort[]>();
		int[] array3 = (existingInstance.Indices = new int[array.Length]);
		int[] array4 = array3;
		for (int i = 0; i < array.Length; i++)
		{
			array4[i] = array[i];
		}
		return existingInstance;
	}
}
