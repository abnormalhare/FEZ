using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class FishEyeEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedVector2 intensity;

	public float Intensity { get; set; }

	public FishEyeEffect()
		: base("ScreenSpaceFisheye")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		intensity = new SemanticMappedVector2(effect.Parameters, "Intensity");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		material.Diffuse = mesh.Material.Diffuse;
		material.Opacity = mesh.Material.Opacity;
		intensity.Set(new Vector2(Intensity / base.GraphicsDeviceService.GraphicsDevice.Viewport.AspectRatio, Intensity) * 0.05f);
		texture.Set(mesh.Texture);
	}
}
