using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class FarawayEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedSingle actualOpacity;

	public float ActualOpacity
	{
		set
		{
			actualOpacity.Set(value);
		}
	}

	public FarawayEffect()
		: base("FarawayEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		actualOpacity = new SemanticMappedSingle(effect.Parameters, "ActualOpacity");
		ActualOpacity = 1f;
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		material.Diffuse = mesh.Material.Diffuse;
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		material.Opacity = group.Material.Opacity;
		texture.Set(group.Texture);
	}

	public void CleanUp()
	{
		texture.Set(null);
	}
}
