using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FezVertexPositionColor : IEquatable<FezVertexPositionColor>, IColoredVertex, IVertex, IVertexType
{
	public static readonly VertexDeclaration vertexDeclaration;

	private Vector3 position;

	private Color color;

	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
		}
	}

	public Color Color
	{
		get
		{
			return color;
		}
		set
		{
			color = value;
		}
	}

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static FezVertexPositionColor()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0));
	}

	public FezVertexPositionColor(Vector3 position, Color color)
	{
		this = default(FezVertexPositionColor);
		this.position = position;
		this.color = color;
	}

	public override string ToString()
	{
		return $"{{Position:{position} Color:{color}}}";
	}

	public bool Equals(FezVertexPositionColor other)
	{
		if (other.position == position)
		{
			return other.color == color;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return position.GetHashCode() ^ color.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((FezVertexPositionColor)obj);
		}
		return false;
	}
}
