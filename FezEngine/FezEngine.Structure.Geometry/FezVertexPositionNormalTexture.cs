using System;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FezVertexPositionNormalTexture : IEquatable<FezVertexPositionNormalTexture>, ILitVertex, IVertex, IVertexType, ITexturedVertex
{
	private static readonly VertexDeclaration vertexDeclaration;

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	public Vector3 Position { get; set; }

	public Vector3 Normal { get; set; }

	public Vector2 TextureCoordinate { get; set; }

	static FezVertexPositionNormalTexture()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
	}

	public FezVertexPositionNormalTexture(Vector3 position, Vector3 normal)
	{
		this = default(FezVertexPositionNormalTexture);
		Position = position;
		Normal = normal;
	}

	public FezVertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texCoord)
		: this(position, normal)
	{
		TextureCoordinate = texCoord;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(FezVertexPositionNormalTexture other)
	{
		if (other.Position == Position)
		{
			return other.Normal == Normal;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ Normal.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((FezVertexPositionNormalTexture)obj);
		}
		return false;
	}
}
