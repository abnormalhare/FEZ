using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedMatrix : SemanticMappedParameter<Matrix>
{
	public SemanticMappedMatrix(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName)
	{
	}

	protected override void DoSet(Matrix value)
	{
		parameter.SetValue(value);
	}
}
