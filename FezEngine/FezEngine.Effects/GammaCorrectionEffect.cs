using FezEngine.Effects.Structures;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class GammaCorrectionEffect : BaseEffect
{
	private readonly SemanticMappedSingle brightness;

	private readonly SemanticMappedTexture mainBufferTexture;

	public float Brightness
	{
		get
		{
			return brightness.Get();
		}
		set
		{
			brightness.Set(value);
		}
	}

	public Texture MainBufferTexture
	{
		get
		{
			return mainBufferTexture.Get();
		}
		set
		{
			mainBufferTexture.Set(value);
		}
	}

	public GammaCorrectionEffect()
		: base("GammaCorrection")
	{
		mainBufferTexture = new SemanticMappedTexture(effect.Parameters, "MainBufferTexture");
		brightness = new SemanticMappedSingle(effect.Parameters, "Brightness");
	}
}
