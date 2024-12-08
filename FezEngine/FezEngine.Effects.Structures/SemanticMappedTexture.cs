using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public class SemanticMappedTexture : SemanticMappedParameter<Texture>
{
	public SemanticMappedTexture(EffectParameterCollection parent, string semanticName)
		: base(parent, semanticName.Replace("Texture", "Sampler"))
	{
	}

	protected override void DoSet(Texture value)
	{
		if (value == null)
		{
			parameter.SetValue(value);
			return;
		}
		if (value.IsDisposed)
		{
			parameter.SetValue((Texture)null);
			return;
		}
		((HashSet<SemanticMappedTexture>)((value.Tag != null) ? (value.Tag as HashSet<SemanticMappedTexture>) : (value.Tag = new HashSet<SemanticMappedTexture>())))?.Add(this);
		parameter.SetValue(value);
	}
}
