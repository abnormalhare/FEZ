using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedVector2 : SemanticMappedParameter<Vector2>
{
	public SemanticMappedVector2(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(Vector2 value)
	{
		parameter.SetValue(value);
	}
}
