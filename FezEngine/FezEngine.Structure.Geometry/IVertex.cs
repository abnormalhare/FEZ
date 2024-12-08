using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public interface IVertex : IVertexType
{
	Vector3 Position { get; set; }
}
