using FezEngine.Effects.Structures;
using FezEngine.Structure;
using FezEngine.Tools;

namespace FezEngine.Effects;

public class FakePointSpritesEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedSingle viewScale;

	private bool groupTextureDirty;

	public FakePointSpritesEffect()
		: base("FakePointSpritesEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		viewScale = new SemanticMappedSingle(effect.Parameters, "ViewScale");
	}

	public override BaseEffect Clone()
	{
		return new FakePointSpritesEffect();
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
		viewScale.Set(base.GraphicsDeviceService.GraphicsDevice.GetViewScale());
		groupTextureDirty = false;
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (group.TexturingType == TexturingType.Texture2D)
		{
			texture.Set(group.Texture);
			groupTextureDirty = true;
		}
		else if (groupTextureDirty)
		{
			texture.Set(group.Mesh.Texture);
		}
	}
}
