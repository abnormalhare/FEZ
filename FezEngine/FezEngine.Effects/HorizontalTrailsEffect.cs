using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class HorizontalTrailsEffect : BaseEffect
{
	private SemanticMappedSingle timing;

	private SemanticMappedVector3 right;

	public float Timing
	{
		get
		{
			return timing.Get();
		}
		set
		{
			timing.Set(value);
		}
	}

	public HorizontalTrailsEffect()
		: base("HorizontalTrailsEffect")
	{
		timing = new SemanticMappedSingle(effect.Parameters, "Timing");
		right = new SemanticMappedVector3(effect.Parameters, "Right");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		right.Set(base.CameraProvider.InverseView.Right);
	}
}
