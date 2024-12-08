using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class ScanlineEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	public ScanlineEffect()
		: base("ScanlineEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		material.Opacity = mesh.Material.Opacity;
		texture.Set(mesh.Texture);
	}
}
