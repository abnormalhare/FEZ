using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Structure;

internal class HowToPlayMenuLevel : MenuLevel
{
	private Texture2D HowToPlayImage;

	private Texture2D HowToPlayImageSony;

	public ITargetRenderingManager TargetRenderer { private get; set; }

	public override void Initialize()
	{
		HowToPlayImage = base.CMProvider.Get(CM.Menu).Load<Texture2D>("Other Textures/how_to_play/howtoplay");
		HowToPlayImageSony = base.CMProvider.Get(CM.Menu).Load<Texture2D>("Other Textures/how_to_play/howtoplay_SONY");
		base.Initialize();
	}

	public override void PostDraw(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha)
	{
		float num = 4f * batch.GraphicsDevice.GetViewScale();
		Vector2 vector = new Vector2(batch.GraphicsDevice.Viewport.Width, batch.GraphicsDevice.Viewport.Height) / 2f;
		Vector2 vector2 = new Vector2(HowToPlayImage.Width, HowToPlayImage.Height) * num;
		batch.End();
		batch.BeginPoint();
		Vector2 position = vector - vector2 / 2f;
		if (GamepadState.Layout != 0)
		{
			batch.Draw(HowToPlayImageSony, position, null, new Color(1f, 1f, 1f, alpha), 0f, Vector2.Zero, num, SpriteEffects.None, 0f);
		}
		else
		{
			batch.Draw(HowToPlayImage, position, null, new Color(1f, 1f, 1f, alpha), 0f, Vector2.Zero, num, SpriteEffects.None, 0f);
		}
	}
}
