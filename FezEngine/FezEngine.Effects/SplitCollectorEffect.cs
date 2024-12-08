using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class SplitCollectorEffect : BaseEffect
{
	private readonly SemanticMappedSingle varyingOpacity;

	private readonly SemanticMappedSingle offset;

	public float VaryingOpacity
	{
		set
		{
			varyingOpacity.Set(value);
		}
	}

	public float Offset
	{
		set
		{
			offset.Set(value);
		}
	}

	public SplitCollectorEffect()
		: base("SplitCollectorEffect")
	{
		varyingOpacity = new SemanticMappedSingle(effect.Parameters, "VaryingOpacity");
		offset = new SemanticMappedSingle(effect.Parameters, "Offset");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		matrices.World = mesh.WorldMatrix;
		material.Diffuse = mesh.Material.Diffuse;
	}
}
