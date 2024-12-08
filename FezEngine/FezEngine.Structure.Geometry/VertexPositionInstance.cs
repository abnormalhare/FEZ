using System;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionInstance : IEquatable<VertexPositionInstance>, IVertex, IVertexType, IShaderInstantiatableVertex
{
	private static readonly VertexDeclaration vertexDeclaration;

	public Vector3 Position { get; set; }

	public float InstanceIndex { get; set; }

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static VertexPositionInstance()
	{
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 0));
	}

	public VertexPositionInstance(Vector3 position)
	{
		this = default(VertexPositionInstance);
		Position = position;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(VertexPositionInstance other)
	{
		if (other.Position == Position)
		{
			return other.InstanceIndex == InstanceIndex;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ InstanceIndex.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((VertexPositionInstance)obj);
		}
		return false;
	}
}
