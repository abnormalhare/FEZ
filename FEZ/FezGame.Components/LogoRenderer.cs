using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class LogoRenderer : DrawableGameComponent
{
	private Texture2D TrapdoorLogo;

	private SpriteBatch SpriteBatch;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public LogoRenderer(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		TrapdoorLogo = CMProvider.Global.Load<Texture2D>("Other Textures/splash/trapdoor");
		SpriteBatch = new SpriteBatch(base.GraphicsDevice);
	}

	public override void Draw(GameTime gameTime)
	{
		base.Draw(gameTime);
		Vector2 vector = (new Vector2(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height) / 2f).Round();
		base.GraphicsDevice.Clear(Color.White);
		SpriteBatch.BeginPoint();
		SpriteBatch.Draw(TrapdoorLogo, vector - (new Vector2(TrapdoorLogo.Width, TrapdoorLogo.Height) / 2f).Round(), new Color(1f, 1f, 1f, 1f));
		SpriteBatch.End();
	}
}
