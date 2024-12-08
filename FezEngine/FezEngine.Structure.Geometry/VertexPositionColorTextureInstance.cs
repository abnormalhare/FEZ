using System;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionColorTextureInstance : IEquatable<VertexPositionColorTextureInstance>, IShaderInstantiatableVertex, ITexturedVertex, IVertex, IVertexType, IColoredVertex
{
	public static readonly VertexDeclaration vertexDeclaration;

	public Vector3 Position { get; set; }

	public Color Color { get; set; }

	public Vector2 TextureCoordinate { get; set; }

	public float InstanceIndex { get; set; }

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static VertexPositionColorTextureInstance()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0), new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1));
	}

	public VertexPositionColorTextureInstance(Vector3 position, Color color, Vector2 textureCoordinate)
	{
		this = default(VertexPositionColorTextureInstance);
		Position = position;
		Color = color;
		TextureCoordinate = textureCoordinate;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(VertexPositionColorTextureInstance other)
	{
		if (other.Position == Position && other.Color == Color && other.TextureCoordinate == TextureCoordinate)
		{
			return other.InstanceIndex == InstanceIndex;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ Color.GetHashCode() ^ TextureCoordinate.GetHashCode() ^ InstanceIndex.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((VertexPositionColorTextureInstance)obj);
		}
		return false;
	}
}
