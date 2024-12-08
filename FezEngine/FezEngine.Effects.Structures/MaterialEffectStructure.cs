using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

internal class MaterialEffectStructure
{
	private readonly SemanticMappedVector3 diffuse;

	private readonly SemanticMappedSingle opacity;

	public Vector3 Diffuse
	{
		set
		{
			diffuse.Set(value);
		}
	}

	public float Opacity
	{
		set
		{
			opacity.Set(value);
		}
	}

	public MaterialEffectStructure(EffectParameterCollection parameters)
	{
		diffuse = new SemanticMappedVector3(parameters, "Material_Diffuse");
		opacity = new SemanticMappedSingle(parameters, "Material_Opacity");
	}
}
