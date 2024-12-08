using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

internal class FogEffectStructure
{
	private readonly SemanticMappedInt32 fogType;

	private readonly SemanticMappedVector3 fogColor;

	private readonly SemanticMappedSingle fogDensity;

	public FogType FogType
	{
		set
		{
			fogType.Set((int)value);
		}
	}

	public Color FogColor
	{
		set
		{
			fogColor.Set(value.ToVector3());
		}
	}

	public float FogDensity
	{
		set
		{
			fogDensity.Set(value);
		}
	}

	public FogEffectStructure(EffectParameterCollection parameters)
	{
		fogType = new SemanticMappedInt32(parameters, "Fog_Type");
		fogColor = new SemanticMappedVector3(parameters, "Fog_Color");
		fogDensity = new SemanticMappedSingle(parameters, "Fog_Density");
	}
}
