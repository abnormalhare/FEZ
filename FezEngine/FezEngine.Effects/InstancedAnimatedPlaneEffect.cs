using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class InstancedAnimatedPlaneEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
{
	private readonly SemanticMappedTexture animatedTexture;

	private readonly SemanticMappedVector2 frameScale;

	private readonly SemanticMappedBoolean ignoreFog;

	private readonly SemanticMappedBoolean sewerHax;

	private readonly SemanticMappedBoolean ignoreShading;

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

	public bool IgnoreShading
	{
		set
		{
			ignoreShading.Set(value);
		}
	}

	public InstancedAnimatedPlaneEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwInstancedAnimatedPlaneEffect" : "InstancedAnimatedPlaneEffect")
	{
		animatedTexture = new SemanticMappedTexture(effect.Parameters, "AnimatedTexture");
		ignoreFog = new SemanticMappedBoolean(effect.Parameters, "IgnoreFog");
		sewerHax = new SemanticMappedBoolean(effect.Parameters, "SewerHax");
		ignoreShading = new SemanticMappedBoolean(effect.Parameters, "IgnoreShading");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedMatrixArray(effect.Parameters, "InstanceData");
		}
		frameScale = new SemanticMappedVector2(effect.Parameters, "FrameScale");
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
		animatedTexture.Set(group.Texture);
		frameScale.Set((Vector2)group.CustomData);
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
