using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class CloudsEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	public CloudsEffect()
		: base("CloudsEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		if (mesh.Texture.Dirty)
		{
			texture.Set(mesh.TextureMap);
			mesh.Texture.Clean();
		}
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		material.Opacity = group.Material.Opacity * group.Mesh.Material.Opacity;
		matrices.World = group.WorldMatrix;
	}
}
