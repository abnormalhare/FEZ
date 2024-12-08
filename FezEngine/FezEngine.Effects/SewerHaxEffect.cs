using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class SewerHaxEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	public SewerHaxEffect()
		: base("SewerHaxEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
	}
}
