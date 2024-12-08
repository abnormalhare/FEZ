using System;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionNormalTextureInstance : IEquatable<VertexPositionNormalTextureInstance>, IShaderInstantiatableVertex, ILitVertex, IVertex, IVertexType, ITexturedVertex
{
	private static readonly VertexDeclaration vertexDeclaration;

	public static readonly Vector3[] ByteToNormal;

	private Vector3 position;

	private Vector3 normal;

	private Vector2 textureCoordinate;

	private float instanceIndex;

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

	public Vector3 Normal
	{
		get
		{
			return normal;
		}
		set
		{
			normal = value;
		}
	}

	public Vector2 TextureCoordinate
	{
		get
		{
			return textureCoordinate;
		}
		set
		{
			textureCoordinate = value;
		}
	}

	public float InstanceIndex
	{
		get
		{
			return instanceIndex;
		}
		set
		{
			instanceIndex = value;
		}
	}

	public VertexDeclaration VertexDeclaration => vertexDeclaration;

	static VertexPositionNormalTextureInstance()
	{
		ByteToNormal = new Vector3[6]
		{
			Vector3.Left,
			Vector3.Down,
			Vector3.Forward,
			Vector3.Right,
			Vector3.Up,
			Vector3.Backward
		};
		vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), new VertexElement(32, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1));
	}

	public VertexPositionNormalTextureInstance(Vector3 position, Vector3 normal)
		: this(position, normal, -1f)
	{
	}

	public VertexPositionNormalTextureInstance(Vector3 position, byte normal, Vector2 textureCoordinate)
	{
		this = default(VertexPositionNormalTextureInstance);
		this.position = position;
		this.normal = ByteToNormal[normal];
		this.textureCoordinate = textureCoordinate;
		instanceIndex = -1f;
	}

	public VertexPositionNormalTextureInstance(Vector3 position, Vector3 normal, float instanceIndex)
	{
		this = default(VertexPositionNormalTextureInstance);
		Position = position;
		Normal = normal;
		InstanceIndex = instanceIndex;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(VertexPositionNormalTextureInstance other)
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
			return Equals((VertexPositionNormalTextureInstance)obj);
		}
		return false;
	}
}
