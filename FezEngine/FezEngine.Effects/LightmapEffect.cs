using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class LightmapEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedBoolean shadowPass;

	public bool ShadowPass
	{
		get
		{
			return shadowPass.Get();
		}
		set
		{
			shadowPass.Set(value);
		}
	}

	public LightmapEffect()
		: base("LightmapEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		shadowPass = new SemanticMappedBoolean(effect.Parameters, "ShadowPass");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
	}
}
