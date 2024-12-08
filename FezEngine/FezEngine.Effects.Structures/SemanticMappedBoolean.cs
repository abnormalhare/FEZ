using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedBoolean : SemanticMappedParameter<bool>
{
	public SemanticMappedBoolean(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(bool value)
	{
		parameter.SetValue(value ? 1f : 0f);
		currentValue = value;
	}
}
