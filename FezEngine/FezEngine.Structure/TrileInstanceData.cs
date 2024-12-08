using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public struct TrileInstanceData
{
	private static readonly VertexDeclaration declaration;

	public Vector4 PositionPhi;

	public static int SizeInBytes => 16;

	public VertexDeclaration VertexDeclaration => declaration;

	static TrileInstanceData()
	{
		declaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1));
	}

	public TrileInstanceData(Vector3 position, float phi)
	{
		PositionPhi = new Vector4(position, phi);
	}

	public override string ToString()
	{
		return $"{{PositionPhi:{PositionPhi}}}";
	}

	public bool Equals(TrileInstanceData other)
	{
		return other.PositionPhi.Equals(PositionPhi);
	}
}
