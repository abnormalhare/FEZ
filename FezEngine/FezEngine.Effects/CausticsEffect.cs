using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class CausticsEffect : BaseEffect
{
	private readonly SemanticMappedTexture animatedTexture;

	private readonly SemanticMappedMatrix nextFrameTextureMatrix;

	public CausticsEffect()
		: base("CausticsEffect")
	{
		animatedTexture = new SemanticMappedTexture(effect.Parameters, "AnimatedTexture");
		nextFrameTextureMatrix = new SemanticMappedMatrix(effect.Parameters, "NextFrameData");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		if (mesh.CustomData != null)
		{
			Matrix value = (Matrix)mesh.CustomData;
			nextFrameTextureMatrix.Set(value);
		}
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (IgnoreCache || !group.EffectOwner || group.InverseTransposeWorldMatrix.Dirty)
		{
			matrices.WorldInverseTranspose = group.InverseTransposeWorldMatrix;
			group.InverseTransposeWorldMatrix.Clean();
		}
		material.Diffuse = group.Material.Diffuse;
		animatedTexture.Set(group.Texture);
	}
}
