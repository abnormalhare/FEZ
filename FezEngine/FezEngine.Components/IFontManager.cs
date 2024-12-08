using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public interface IFontManager
{
	SpriteFont Big { get; }

	SpriteFont Small { get; }

	float SmallFactor { get; }

	float BigFactor { get; }

	float TopSpacing { get; }

	float SideSpacing { get; }
}
