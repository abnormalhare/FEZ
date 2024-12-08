using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class VignetteEffect : BaseEffect
{
	private readonly SemanticMappedSingle sinceStarted;

	public float SinceStarted
	{
		get
		{
			return sinceStarted.Get();
		}
		set
		{
			sinceStarted.Set(value);
		}
	}

	public VignetteEffect()
		: base("VignetteEffect")
	{
		sinceStarted = new SemanticMappedSingle(effect.Parameters, "SinceStarted");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		material.Opacity = mesh.Material.Opacity;
	}
}
