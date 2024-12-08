using FezEngine.Effects.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class CombineEffect : BaseEffect
{
	private readonly SemanticMappedTexture rightTexture;

	private readonly SemanticMappedTexture leftTexture;

	private readonly SemanticMappedSingle redGamma;

	private readonly SemanticMappedMatrix rightFilter;

	private readonly SemanticMappedMatrix leftFilter;

	public Texture2D LeftTexture
	{
		set
		{
			leftTexture.Set(value);
		}
	}

	public Texture2D RightTexture
	{
		set
		{
			rightTexture.Set(value);
		}
	}

	public float RedGamma
	{
		set
		{
			redGamma.Set(value);
		}
	}

	public Matrix RightFilter
	{
		set
		{
			rightFilter.Set(value);
		}
	}

	public Matrix LeftFilter
	{
		set
		{
			leftFilter.Set(value);
		}
	}

	public CombineEffect()
		: base("CombineEffect")
	{
		rightTexture = new SemanticMappedTexture(effect.Parameters, "RightTexture");
		leftTexture = new SemanticMappedTexture(effect.Parameters, "LeftTexture");
		redGamma = new SemanticMappedSingle(effect.Parameters, "RedGamma");
		rightFilter = new SemanticMappedMatrix(effect.Parameters, "RightFilter");
		leftFilter = new SemanticMappedMatrix(effect.Parameters, "LeftFilter");
		LeftFilter = new Matrix(0.2125f, 0.7154f, 0.0721f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
		RightFilter = new Matrix(0f, 0f, 0f, 0f, 0.2125f, 0.7154f, 0.0721f, 0f, 0.2125f, 0.7154f, 0.0721f, 0f, 0f, 0f, 0f, 0f);
	}
}
