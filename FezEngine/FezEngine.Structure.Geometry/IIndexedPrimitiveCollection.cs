using FezEngine.Effects;

namespace FezEngine.Structure.Geometry;

public interface IIndexedPrimitiveCollection
{
	bool Empty { get; }

	int VertexCount { get; }

	void Draw(BaseEffect effect);

	IIndexedPrimitiveCollection Clone();
}
