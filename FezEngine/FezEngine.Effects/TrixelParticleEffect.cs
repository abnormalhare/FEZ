using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class TrixelParticleEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedMatrixArray instanceData;

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value != 0) ? 1 : 0];
		}
	}

	public TrixelParticleEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwTrixelParticleEffect" : "TrixelParticleEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedMatrixArray(effect.Parameters, "InstanceData");
		}
		Pass = LightingEffectPass.Main;
		SimpleMeshPrepare = (SimpleGroupPrepare = true);
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
