using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class FastBlurEffect : BaseEffect
{
	private readonly SemanticMappedVector2 texelSize;

	private readonly SemanticMappedVector2 direction;

	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedSingle blurWidth;

	public BlurPass Pass
	{
		set
		{
			if (value == BlurPass.Horizontal)
			{
				direction.Set(Vector2.UnitX);
			}
			if (value == BlurPass.Vertical)
			{
				direction.Set(Vector2.UnitY);
			}
		}
	}

	public float BlurWidth
	{
		set
		{
			blurWidth.Set(value);
		}
	}

	public FastBlurEffect()
		: base("FastBlurEffect")
	{
		texelSize = new SemanticMappedVector2(effect.Parameters, "TexelSize");
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		blurWidth = new SemanticMappedSingle(effect.Parameters, "BlurWidth");
		direction = new SemanticMappedVector2(effect.Parameters, "Direction");
		effect.Parameters["Weights"].SetValue(new float[5] { 0.08812122f, 0.16755535f, 0.13691124f, 0.09517907f, 0.056293722f });
		effect.Parameters["Offsets"].SetValue(new float[5] { 0f, -0.015299783f, -0.035650045f, -0.055882283f, -0.075930886f });
		BlurWidth = 1f;
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
		texelSize.Set(new Vector2(1f / (float)mesh.TextureMap.Width, 1f / (float)mesh.TextureMap.Height));
	}
}
