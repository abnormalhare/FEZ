using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class SkyBackEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	public SkyBackEffect()
		: base("SkyBackEffect")
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

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		matrices.World = group.WorldMatrix;
	}
}
