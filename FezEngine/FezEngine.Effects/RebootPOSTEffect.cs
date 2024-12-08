using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class RebootPOSTEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedMatrix pseudoWorldMatrix;

	public Matrix PseudoWorld
	{
		set
		{
			pseudoWorldMatrix.Set(value);
		}
	}

	public RebootPOSTEffect()
		: base("RebootPOSTEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		pseudoWorldMatrix = new SemanticMappedMatrix(effect.Parameters, "PseudoWorldMatrix");
		PseudoWorld = Matrix.Identity;
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		material.Diffuse = mesh.Material.Diffuse;
		material.Opacity = mesh.Material.Opacity;
		texture.Set(mesh.Texture);
	}
}
