using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedInt32 : SemanticMappedParameter<int>
{
	public SemanticMappedInt32(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(int value)
	{
		parameter.SetValue((float)value);
		currentValue = value;
	}
}
