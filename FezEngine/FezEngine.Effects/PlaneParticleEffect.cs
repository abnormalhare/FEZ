using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class PlaneParticleEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedBoolean additive;

	private readonly SemanticMappedBoolean fullbright;

	private readonly SemanticMappedMatrixArray instanceData;

	public Matrix? ForcedViewProjection { get; set; }

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value != 0) ? 1 : 0];
		}
	}

	public bool Additive
	{
		set
		{
			additive.Set(value);
		}
	}

	public bool Fullbright
	{
		set
		{
			fullbright.Set(value);
		}
	}

	public PlaneParticleEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwPlaneParticleEffect" : "PlaneParticleEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		additive = new SemanticMappedBoolean(effect.Parameters, "Additive");
		fullbright = new SemanticMappedBoolean(effect.Parameters, "Fullbright");
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
		if (ForcedViewProjection.HasValue)
		{
			matrices.WorldViewProjection = ForcedViewProjection.Value;
		}
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
