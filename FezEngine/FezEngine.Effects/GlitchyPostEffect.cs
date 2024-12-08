using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class GlitchyPostEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
{
	private readonly SemanticMappedTexture glitchTexture;

	private readonly SemanticMappedMatrixArray instanceData;

	public GlitchyPostEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwGlitchyPostEffect" : "GlitchyPostEffect")
	{
		glitchTexture = new SemanticMappedTexture(effect.Parameters, "GlitchTexture");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedMatrixArray(effect.Parameters, "InstanceData");
		}
		SimpleGroupPrepare = true;
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		glitchTexture.Set(mesh.Texture);
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		Apply();
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
