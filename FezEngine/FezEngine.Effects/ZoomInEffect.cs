using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class ZoomInEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	public ZoomInEffect()
		: base("ZoomInEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		material.Diffuse = mesh.Material.Diffuse;
		material.Opacity = mesh.Material.Opacity;
		texture.Set(mesh.Texture);
	}
}
