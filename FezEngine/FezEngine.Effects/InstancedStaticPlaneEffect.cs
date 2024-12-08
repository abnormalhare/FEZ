using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class InstancedStaticPlaneEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
{
	private readonly SemanticMappedTexture baseTexture;

	private readonly SemanticMappedBoolean ignoreFog;

	private readonly SemanticMappedBoolean sewerHax;

	private readonly SemanticMappedMatrixArray instanceData;

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value != 0) ? 1 : 0];
		}
	}

	public bool IgnoreFog
	{
		set
		{
			ignoreFog.Set(value);
		}
	}

	public InstancedStaticPlaneEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwInstancedStaticPlaneEffect" : "InstancedStaticPlaneEffect")
	{
		baseTexture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		ignoreFog = new SemanticMappedBoolean(effect.Parameters, "IgnoreFog");
		sewerHax = new SemanticMappedBoolean(effect.Parameters, "SewerHax");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedMatrixArray(effect.Parameters, "InstanceData");
		}
		Pass = LightingEffectPass.Main;
	}

	public override void Prepare(Mesh mesh)
	{
		sewerHax.Set(base.LevelManager.WaterType == LiquidType.Sewer);
		matrices.WorldViewProjection = viewProjection;
		base.Prepare(mesh);
	}

	public override void Prepare(Group group)
	{
		baseTexture.Set(group.Texture);
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
