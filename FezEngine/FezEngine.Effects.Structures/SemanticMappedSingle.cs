using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedSingle : SemanticMappedParameter<float>
{
	public SemanticMappedSingle(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(float value)
	{
		parameter.SetValue(value);
		currentValue = value;
	}
}
