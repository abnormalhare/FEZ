using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FezVertexPositionTexture : IEquatable<FezVertexPositionTexture>, ITexturedVertex, IVertex, IVertexType
{
	public static readonly VertexDeclaration vertexDeclaration;

	public Vector3 Position { get; set; }

	public Vector2 TextureCoordinate { get; set; }

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static FezVertexPositionTexture()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
	}

	public FezVertexPositionTexture(Vector3 position, Vector2 textureCoordinate)
	{
		this = default(FezVertexPositionTexture);
		Position = position;
		TextureCoordinate = textureCoordinate;
	}

	public override string ToString()
	{
		return $"{{Position:{Position} TextureCoordinate:{TextureCoordinate}}}";
	}

	public bool Equals(FezVertexPositionTexture other)
	{
		if (other.Position.Equals(Position))
		{
			return other.TextureCoordinate.Equals(TextureCoordinate);
		}
		return false;
	}
}
