using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedVector3 : SemanticMappedParameter<Vector3>
{
	public SemanticMappedVector3(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(Vector3 value)
	{
		parameter.SetValue(value);
		firstSet = false;
	}
}
