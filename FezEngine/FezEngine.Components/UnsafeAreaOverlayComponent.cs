using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class UnsafeAreaOverlayComponent : DrawableGameComponent
{
	private Color noActionAreaColor;

	private Color unsafeAreaColor;

	private SpriteBatch spriteBatch;

	private Texture2D texture;

	private Rectangle[] noActionAreaParts;

	private Rectangle[] unsafeAreaParts;

	public Color NoActionAreaColor
	{
		get
		{
			return noActionAreaColor;
		}
		set
		{
			noActionAreaColor = value;
		}
	}

	public Color UnsafeAreaColor
	{
		get
		{
			return unsafeAreaColor;
		}
		set
		{
			unsafeAreaColor = value;
		}
	}

	public UnsafeAreaOverlayComponent(Game game)
		: base(game)
	{
		base.DrawOrder = int.MaxValue;
		noActionAreaColor = new Color(255, 0, 0, 127);
		unsafeAreaColor = new Color(255, 255, 0, 127);
	}

	protected override void LoadContent()
	{
		spriteBatch = new SpriteBatch(base.GraphicsDevice);
		texture = new Texture2D(base.GraphicsDevice, 1, 1, mipMap: false, SurfaceFormat.Color);
		Color[] data = new Color[1] { Color.White };
		texture.SetData(data);
		int width = base.GraphicsDevice.Viewport.Width;
		int height = base.GraphicsDevice.Viewport.Height;
		int num = (int)((double)width * 0.05);
		int num2 = (int)((double)height * 0.05);
		noActionAreaParts = new Rectangle[4];
		noActionAreaParts[0] = new Rectangle(0, 0, width, num2);
		noActionAreaParts[1] = new Rectangle(0, height - num2, width, num2);
		noActionAreaParts[2] = new Rectangle(0, num2, num, height - 2 * num2);
		noActionAreaParts[3] = new Rectangle(width - num, num2, num, height - 2 * num2);
		unsafeAreaParts = new Rectangle[4];
		unsafeAreaParts[0] = new Rectangle(num, num2, width - 2 * num, num2);
		unsafeAreaParts[1] = new Rectangle(num, height - 2 * num2, width - 2 * num, num2);
		unsafeAreaParts[2] = new Rectangle(num, 2 * num2, num, height - 4 * num2);
		unsafeAreaParts[3] = new Rectangle(width - 2 * num, 2 * num2, num, height - 4 * num2);
	}

	public override void Draw(GameTime gameTime)
	{
		spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
		for (int i = 0; i < noActionAreaParts.Length; i++)
		{
			spriteBatch.Draw(texture, noActionAreaParts[i], noActionAreaColor);
		}
		for (int j = 0; j < unsafeAreaParts.Length; j++)
		{
			spriteBatch.Draw(texture, unsafeAreaParts[j], unsafeAreaColor);
		}
		spriteBatch.End();
	}
}
