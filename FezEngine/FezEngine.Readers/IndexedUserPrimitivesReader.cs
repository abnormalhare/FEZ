using FezEngine.Structure.Geometry;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Readers;

public class IndexedUserPrimitivesReader<T> : ContentTypeReader<IndexedUserPrimitives<T>> where T : struct, IVertexType
{
	protected override IndexedUserPrimitives<T> Read(ContentReader input, IndexedUserPrimitives<T> existingInstance)
	{
		PrimitiveType type = input.ReadObject<PrimitiveType>();
		T[] vertices = input.ReadObject<T[]>();
		int[] indices = input.ReadObject<int[]>();
		return new IndexedUserPrimitives<T>(vertices, indices, type);
	}
}
