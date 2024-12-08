using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexFakePointSprite : IEquatable<VertexFakePointSprite>, IColoredVertex, IVertex, IVertexType, ITexturedVertex
{
	private static readonly VertexDeclaration vertexDeclaration;

	private Vector3 _position;

	private Color _color;

	private Vector2 _textureCoordinate;

	private Vector2 _offset;

	public Vector3 Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = value;
		}
	}

	public Color Color
	{
		get
		{
			return _color;
		}
		set
		{
			_color = value;
		}
	}

	public Vector2 TextureCoordinate
	{
		get
		{
			return _textureCoordinate;
		}
		set
		{
			_textureCoordinate = value;
		}
	}

	public Vector2 Offset
	{
		get
		{
			return _offset;
		}
		set
		{
			_offset = value;
		}
	}

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static VertexFakePointSprite()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0), new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1));
	}

	public VertexFakePointSprite(Vector3 centerPosition, Color color, Vector2 texCoord, Vector2 offset)
	{
		this = default(VertexFakePointSprite);
		_position = centerPosition;
		_color = color;
		_textureCoordinate = texCoord;
		_offset = offset;
	}

	public bool Equals(VertexFakePointSprite other)
	{
		if (other.Position == Position && other.Color == Color && other.TextureCoordinate == TextureCoordinate)
		{
			return other.Offset == Offset;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ Color.GetHashCode() ^ TextureCoordinate.GetHashCode() ^ Offset.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((VertexFakePointSprite)obj);
		}
		return false;
	}
}
