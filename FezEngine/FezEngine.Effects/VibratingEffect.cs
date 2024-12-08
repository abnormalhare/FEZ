using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class VibratingEffect : BaseEffect
{
	private readonly SemanticMappedSingle intensity;

	private readonly SemanticMappedSingle timeStep;

	private readonly SemanticMappedSingle fogDensity;

	private Vector3 lastDiffuse;

	public float Intensity
	{
		get
		{
			return intensity.Get();
		}
		set
		{
			intensity.Set(value);
		}
	}

	public float TimeStep
	{
		get
		{
			return timeStep.Get();
		}
		set
		{
			timeStep.Set(value);
		}
	}

	public float FogDensity
	{
		get
		{
			return fogDensity.Get();
		}
		set
		{
			fogDensity.Set(value);
		}
	}

	public VibratingEffect()
		: base("VibratingEffect")
	{
		intensity = new SemanticMappedSingle(effect.Parameters, "Intensity");
		timeStep = new SemanticMappedSingle(effect.Parameters, "TimeStep");
		fogDensity = new SemanticMappedSingle(effect.Parameters, "FogDensity");
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (group.Material != null)
		{
			if (lastDiffuse != group.Material.Diffuse)
			{
				material.Diffuse = group.Material.Diffuse;
				lastDiffuse = group.Material.Diffuse;
			}
		}
		else
		{
			material.Diffuse = group.Mesh.Material.Diffuse;
		}
	}
}
