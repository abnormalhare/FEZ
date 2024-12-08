using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class CloudShadowEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	public CloudShadowPasses Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value == CloudShadowPasses.Canopy) ? 1 : 0];
		}
	}

	public CloudShadowEffect()
		: base("CloudShadowEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
	}
}
