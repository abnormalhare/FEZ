using System.Linq;
using FezEngine.Effects;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public class IndexedUserPrimitives<T> : IndexedPrimitiveCollectionBase<T, int> where T : struct, IVertexType
{
	public IndexedUserPrimitives(PrimitiveType type)
		: this((T[])null, (int[])null, type)
	{
	}

	public IndexedUserPrimitives(T[] vertices, int[] indices, PrimitiveType type)
		: base(type)
	{
		base.vertices = vertices ?? new T[0];
		base.Indices = indices ?? new int[0];
	}

	public override void Draw(BaseEffect effect)
	{
		if (device != null && vertices.Length != 0 && !base.Empty)
		{
			effect.Apply();
			device.DrawUserIndexedPrimitives(primitiveType, vertices, 0, vertices.Length, indices, 0, primitiveCount);
		}
	}

	public override IIndexedPrimitiveCollection Clone()
	{
		return new IndexedUserPrimitives<T>(vertices.ToArray(), indices, primitiveType);
	}
}
