using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedVector4 : SemanticMappedParameter<Vector4>
{
	public SemanticMappedVector4(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(Vector4 value)
	{
		parameter.SetValue(value);
	}
}
