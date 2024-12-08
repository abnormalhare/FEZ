using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public interface IColoredVertex : IVertex, IVertexType
{
	Color Color { get; }
}
