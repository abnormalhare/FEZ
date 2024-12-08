using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public static class TextureExtensions
{
	public static void Unhook(this Texture texture)
	{
		if (texture.Tag == null)
		{
			return;
		}
		foreach (SemanticMappedTexture item in texture.Tag as HashSet<SemanticMappedTexture>)
		{
			item.Set(null);
		}
		texture.Tag = null;
	}
}
