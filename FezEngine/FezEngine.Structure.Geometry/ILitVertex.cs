using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public interface ILitVertex : IVertex, IVertexType
{
	Vector3 Normal { get; set; }
}
