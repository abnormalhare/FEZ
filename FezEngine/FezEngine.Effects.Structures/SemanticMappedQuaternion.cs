using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedQuaternion : SemanticMappedParameter<Quaternion>
{
	public SemanticMappedQuaternion(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(Quaternion value)
	{
		parameter.SetValue(value);
	}
}
