using FezEngine.Effects.Structures;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class ShimmeringPointsEffect : BaseEffect
{
	private readonly SemanticMappedVector3 randomSeed;

	private readonly SemanticMappedSingle saturation;

	public float Saturation
	{
		set
		{
			saturation.Set(value);
		}
	}

	public ShimmeringPointsEffect()
		: base("ShimmeringPointsEffect")
	{
		randomSeed = new SemanticMappedVector3(effect.Parameters, "RandomSeed");
		saturation = new SemanticMappedSingle(effect.Parameters, "Saturation");
		saturation.Set(1f);
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		randomSeed.Set(new Vector3(RandomHelper.Unit(), RandomHelper.Unit(), RandomHelper.Unit()));
		material.Diffuse = mesh.Material.Diffuse;
		material.Opacity = mesh.Material.Opacity;
	}
}
