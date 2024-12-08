using System;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionColorInstance : IEquatable<VertexPositionColorInstance>, IShaderInstantiatableVertex, IColoredVertex, IVertex, IVertexType
{
	private static readonly VertexDeclaration vertexDeclaration;

	public Vector3 Position { get; set; }

	public Color Color { get; set; }

	public float InstanceIndex { get; set; }

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static VertexPositionColorInstance()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0), new VertexElement(16, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1));
	}

	public VertexPositionColorInstance(Vector3 position, Color color)
	{
		this = default(VertexPositionColorInstance);
		Position = position;
		Color = color;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(VertexPositionColorInstance other)
	{
		if (other.Position == Position && other.Color == Color)
		{
			return other.InstanceIndex == InstanceIndex;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ Color.GetHashCode() ^ InstanceIndex.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((VertexPositionColorInstance)obj);
		}
		return false;
	}
}
