using FezEngine.Structure.Geometry;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class FezVertexPositionNormalTextureReader : ContentTypeReader<FezVertexPositionNormalTexture>
{
	protected override FezVertexPositionNormalTexture Read(ContentReader input, FezVertexPositionNormalTexture existingInstance)
	{
		FezVertexPositionNormalTexture result = new FezVertexPositionNormalTexture(input.ReadVector3(), input.ReadVector3());
		result.TextureCoordinate = input.ReadVector2();
		return result;
	}
}
