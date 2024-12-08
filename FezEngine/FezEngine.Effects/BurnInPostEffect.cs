using FezEngine.Effects.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class BurnInPostEffect : BaseEffect
{
	private readonly SemanticMappedVector3 acceptColor;

	private readonly SemanticMappedTexture newFrameBuffer;

	private readonly SemanticMappedTexture oldFrameBuffer;

	public Texture OldFrameBuffer
	{
		set
		{
			oldFrameBuffer.Set(value);
		}
	}

	public Texture NewFrameBuffer
	{
		set
		{
			newFrameBuffer.Set(value);
		}
	}

	public BurnInPostEffect()
		: base("BurnInPostEffect")
	{
		acceptColor = new SemanticMappedVector3(effect.Parameters, "AcceptColor");
		acceptColor.Set(new Vector3(14f / 15f, 0f, 47f / 85f));
		oldFrameBuffer = new SemanticMappedTexture(effect.Parameters, "OldFrameTexture");
		newFrameBuffer = new SemanticMappedTexture(effect.Parameters, "NewFrameTexture");
	}
}
