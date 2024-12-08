using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class FakeIntro : DrawableGameComponent
{
	private Texture2D PolyLogo;

	private SpriteBatch SpriteBatch;

	private float logoAlpha;

	private float whiteAlpha;

	private float sinceStarted;

	[ServiceDependency]
	public ITargetRenderingManager TRM { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	public FakeIntro(Game game)
		: base(game)
	{
		base.DrawOrder = 1000;
	}

	protected override void LoadContent()
	{
		PolyLogo = CMProvider.Global.Load<Texture2D>("Other Textures/splash/Polytron Logo");
		SpriteBatch = new SpriteBatch(base.GraphicsDevice);
	}

	public override void Draw(GameTime gameTime)
	{
		sinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds;
		logoAlpha = (sinceStarted - 1f) / 1f;
		whiteAlpha = 1f;
		if (sinceStarted > 3f)
		{
			logoAlpha = FezMath.Saturate(1f - (sinceStarted - 3f) / 0.5f);
		}
		if (sinceStarted > 3.5f)
		{
			whiteAlpha = FezMath.Saturate(1f - (sinceStarted - 4f) / 0.5f);
		}
		TRM.DrawFullscreen(new Color(1f, 1f, 1f, whiteAlpha));
		Vector2 vector = (new Vector2(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height) / 2f).Round();
		SpriteBatch.BeginPoint();
		SpriteBatch.Draw(PolyLogo, vector - (new Vector2(PolyLogo.Width, PolyLogo.Height) / 2f).Round(), new Color(1f, 1f, 1f, logoAlpha));
		SpriteBatch.End();
		if (whiteAlpha == 0f)
		{
			ServiceHelper.RemoveComponent(this);
		}
	}
}
