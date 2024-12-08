using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class InstancedArtObjectEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
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

	public InstancedArtObjectEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwInstancedArtObjectEffect" : "InstancedArtObjectEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "CubemapTexture");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedMatrixArray(effect.Parameters, "InstanceData");
		}
		Pass = LightingEffectPass.Main;
		SimpleMeshPrepare = (SimpleGroupPrepare = true);
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		texture.Set(group.Texture);
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
