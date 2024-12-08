using FezEngine.Structure.Geometry;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class VertexPositionNormalTextureInstanceReader : ContentTypeReader<VertexPositionNormalTextureInstance>
{
	protected override VertexPositionNormalTextureInstance Read(ContentReader input, VertexPositionNormalTextureInstance existingInstance)
	{
		return new VertexPositionNormalTextureInstance(input.ReadVector3(), input.ReadByte(), input.ReadVector2());
	}
}
