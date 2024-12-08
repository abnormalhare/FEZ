using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedString : SemanticMappedParameter<string>
{
	public SemanticMappedString(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(string value)
	{
		parameter.SetValue(value);
	}
}
