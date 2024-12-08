using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionNormalColor : IEquatable<VertexPositionNormalColor>, ILitVertex, IVertex, IVertexType
{
	public static readonly VertexDeclaration vertexDeclaration;

	public Vector3 Position { get; set; }

	public Vector3 Normal { get; set; }

	public Color Color { get; set; }

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static VertexPositionNormalColor()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0));
	}

	public VertexPositionNormalColor(Vector3 position, Vector3 normal, Color color)
	{
		this = default(VertexPositionNormalColor);
		Position = position;
		Normal = normal;
		Color = color;
	}

	public override string ToString()
	{
		return $"{{Position:{Position} Normal:{Normal} Color:{Color}}}";
	}

	public bool Equals(VertexPositionNormalColor other)
	{
		if (other.Position.Equals(Position) && other.Normal.Equals(Normal))
		{
			return other.Color.Equals(Color);
		}
		return false;
	}
}
