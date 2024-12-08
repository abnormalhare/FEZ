using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class GomezEffect : BaseEffect
{
	private readonly SemanticMappedTexture animatedTexture;

	private readonly SemanticMappedBoolean silhouette;

	private readonly SemanticMappedSingle background;

	private readonly SemanticMappedBoolean colorSwap;

	private readonly SemanticMappedBoolean noMoreFez;

	private readonly SemanticMappedVector3 redSwap;

	private readonly SemanticMappedVector3 blackSwap;

	private readonly SemanticMappedVector3 whiteSwap;

	private readonly SemanticMappedVector3 yellowSwap;

	private readonly SemanticMappedVector3 graySwap;

	private ColorSwapMode colorSwapMode;

	public Texture Animation
	{
		set
		{
			animatedTexture.Set(value);
		}
	}

	public bool Silhouette
	{
		set
		{
			silhouette.Set(value);
		}
	}

	public float Background
	{
		set
		{
			background.Set(value);
		}
	}

	public ColorSwapMode ColorSwapMode
	{
		get
		{
			return colorSwapMode;
		}
		set
		{
			colorSwapMode = value;
			switch (colorSwapMode)
			{
			case ColorSwapMode.None:
				colorSwap.Set(value: false);
				break;
			case ColorSwapMode.Gameboy:
				colorSwap.Set(value: true);
				redSwap.Set(new Vector3(0.32156864f, 0.49803922f, 19f / 85f));
				blackSwap.Set(new Vector3(0.1254902f, 14f / 51f, 0.19215687f));
				whiteSwap.Set(new Vector3(43f / 51f, 0.9098039f, 0.5803922f));
				yellowSwap.Set(new Vector3(58f / 85f, 0.76862746f, 0.2509804f));
				graySwap.Set(new Vector3(0.32156864f, 0.49803922f, 19f / 85f));
				break;
			case ColorSwapMode.VirtualBoy:
				colorSwap.Set(value: true);
				redSwap.Set(new Vector3(0.61960787f, 0f, 1f / 51f));
				blackSwap.Set(new Vector3(0f, 0f, 0f));
				whiteSwap.Set(new Vector3(0.99607843f, 0.003921569f, 0f));
				yellowSwap.Set(new Vector3(0.8156863f, 0.003921569f, 0f));
				graySwap.Set(new Vector3(0.39607844f, 0.003921569f, 0f));
				break;
			case ColorSwapMode.Cmyk:
				colorSwap.Set(value: true);
				redSwap.Set(new Vector3(14f / 15f, 0f, 47f / 85f));
				blackSwap.Set(new Vector3(0f, 0f, 0f));
				whiteSwap.Set(new Vector3(1f, 1f, 1f));
				yellowSwap.Set(new Vector3(1f, 1f, 0f));
				graySwap.Set(new Vector3(1f, 1f, 1f));
				break;
			}
		}
	}

	public bool NoMoreFez
	{
		set
		{
			noMoreFez.Set(value);
		}
	}

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value != 0) ? 1 : 0];
		}
	}

	public GomezEffect()
		: base("GomezEffect")
	{
		animatedTexture = new SemanticMappedTexture(effect.Parameters, "AnimatedTexture");
		silhouette = new SemanticMappedBoolean(effect.Parameters, "Silhouette");
		background = new SemanticMappedSingle(effect.Parameters, "Background");
		colorSwap = new SemanticMappedBoolean(effect.Parameters, "ColorSwap");
		redSwap = new SemanticMappedVector3(effect.Parameters, "RedSwap");
		blackSwap = new SemanticMappedVector3(effect.Parameters, "BlackSwap");
		whiteSwap = new SemanticMappedVector3(effect.Parameters, "WhiteSwap");
		yellowSwap = new SemanticMappedVector3(effect.Parameters, "YellowSwap");
		graySwap = new SemanticMappedVector3(effect.Parameters, "GraySwap");
		noMoreFez = new SemanticMappedBoolean(effect.Parameters, "NoMoreFez");
		Pass = LightingEffectPass.Main;
	}

	public override BaseEffect Clone()
	{
		return new GomezEffect
		{
			Animation = animatedTexture.Get(),
			Silhouette = silhouette.Get(),
			Background = background.Get(),
			ColorSwapMode = colorSwapMode
		};
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (IgnoreCache || !group.EffectOwner || group.WorldMatrix.Dirty)
		{
			matrices.World = group.WorldMatrix;
			group.WorldMatrix.Clean();
		}
		if (group.TextureMatrix.Value.HasValue)
		{
			matrices.TextureMatrix = group.TextureMatrix.Value.Value;
			textureMatrixDirty = true;
		}
		else if (textureMatrixDirty)
		{
			matrices.TextureMatrix = Matrix.Identity;
			textureMatrixDirty = false;
		}
	}
}
