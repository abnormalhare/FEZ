using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Structure;

internal class CreditsEntry
{
	public bool IsTitle;

	public bool IsSubtitle;

	public Texture2D Image;

	public string Text;

	public Color Color = Color.White;

	public Vector2 Size;
}
