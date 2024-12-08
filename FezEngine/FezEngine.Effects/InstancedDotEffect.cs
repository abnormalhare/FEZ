using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class InstancedDotEffect : BaseEffect, IShaderInstantiatableEffect<Vector4>
{
	private readonly SemanticMappedSingle theta;

	private readonly SemanticMappedSingle eightShapeStep;

	private readonly SemanticMappedSingle distanceFactor;

	private readonly SemanticMappedSingle immobilityFactor;

	private readonly SemanticMappedVectorArray instanceData;

	public float Theta
	{
		set
		{
			theta.Set(value);
		}
	}

	public float EightShapeStep
	{
		set
		{
			eightShapeStep.Set(value);
		}
	}

	public float DistanceFactor
	{
		set
		{
			distanceFactor.Set(value);
		}
	}

	public float ImmobilityFactor
	{
		set
		{
			immobilityFactor.Set(value);
		}
	}

	public InstancedDotEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwInstancedDotEffect" : "InstancedDotEffect")
	{
		theta = new SemanticMappedSingle(effect.Parameters, "Theta");
		eightShapeStep = new SemanticMappedSingle(effect.Parameters, "EightShapeStep");
		distanceFactor = new SemanticMappedSingle(effect.Parameters, "DistanceFactor");
		immobilityFactor = new SemanticMappedSingle(effect.Parameters, "ImmobilityFactor");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedVectorArray(effect.Parameters, "InstanceData");
		}
		SimpleGroupPrepare = true;
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		material.Diffuse = mesh.Material.Diffuse;
		material.Opacity = mesh.Material.Opacity;
	}

	public void SetInstanceData(Vector4[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
