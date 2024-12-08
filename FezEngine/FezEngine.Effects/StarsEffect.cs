using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class StarsEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	public StarsEffect()
		: base("StarsEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		if (mesh.Texture.Dirty)
		{
			texture.Set(mesh.Texture);
			mesh.Texture.Clean();
		}
	}
}
