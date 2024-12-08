using System;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionTextureInstance : IEquatable<VertexPositionTextureInstance>, IShaderInstantiatableVertex, ITexturedVertex, IVertex, IVertexType
{
	private static readonly VertexDeclaration vertexDeclaration;

	public Vector3 Position { get; set; }

	public Vector2 TextureCoordinate { get; set; }

	public float InstanceIndex { get; set; }

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static VertexPositionTextureInstance()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), new VertexElement(20, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1));
	}

	public VertexPositionTextureInstance(Vector3 position, Vector2 textureCoordinate)
	{
		this = default(VertexPositionTextureInstance);
		Position = position;
		TextureCoordinate = textureCoordinate;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(VertexPositionTextureInstance other)
	{
		if (other.Position == Position && other.TextureCoordinate == TextureCoordinate)
		{
			return other.InstanceIndex == InstanceIndex;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ TextureCoordinate.GetHashCode() ^ InstanceIndex.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((VertexPositionTextureInstance)obj);
		}
		return false;
	}
}
