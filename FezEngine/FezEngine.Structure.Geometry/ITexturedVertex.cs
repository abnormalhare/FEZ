using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public interface ITexturedVertex : IVertex, IVertexType
{
	Vector2 TextureCoordinate { get; set; }
}
