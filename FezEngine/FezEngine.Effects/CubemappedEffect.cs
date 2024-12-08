using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class CubemappedEffect : BaseEffect
{
	private readonly SemanticMappedTexture cubemap;

	private readonly SemanticMappedBoolean forceShading;

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value != 0) ? 1 : 0];
		}
	}

	public bool ForceShading
	{
		set
		{
			forceShading.Set(value);
		}
	}

	public CubemappedEffect()
		: base("CubemappedEffect")
	{
		cubemap = new SemanticMappedTexture(effect.Parameters, "CubemapTexture");
		forceShading = new SemanticMappedBoolean(effect.Parameters, "ForceShading");
		Pass = LightingEffectPass.Main;
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (IgnoreCache || !group.EffectOwner || group.InverseTransposeWorldMatrix.Dirty)
		{
			matrices.WorldInverseTranspose = group.InverseTransposeWorldMatrix;
			group.InverseTransposeWorldMatrix.Clean();
		}
		cubemap.Set(group.Texture);
		material.Diffuse = ((group.Material != null) ? group.Material.Diffuse : group.Mesh.Material.Diffuse);
	}
}
