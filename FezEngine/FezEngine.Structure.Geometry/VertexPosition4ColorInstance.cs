using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPosition4ColorInstance : IEquatable<VertexPosition4ColorInstance>, IShaderInstantiatableVertex, IVertexType
{
	private static readonly VertexDeclaration vertexDeclaration;

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	public Vector4 Position { get; set; }

	public Color Color { get; set; }

	public float InstanceIndex { get; set; }

	public static int SizeInBytes => 24;

	static VertexPosition4ColorInstance()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0), new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0), new VertexElement(20, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 0));
	}

	public VertexPosition4ColorInstance(Vector4 position, Color color)
	{
		this = default(VertexPosition4ColorInstance);
		Position = position;
		Color = color;
	}

	public override string ToString()
	{
		return $"{{Position:{Position} Color:{Color}}}";
	}

	public bool Equals(VertexPosition4ColorInstance other)
	{
		if (other.Position == Position)
		{
			return other.Color == Color;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ Color.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((VertexPosition4ColorInstance)obj);
		}
		return false;
	}
}
