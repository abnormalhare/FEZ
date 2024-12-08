using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class FakeOutro : DrawableGameComponent
{
	private Texture2D HappyLogo;

	private SpriteBatch SpriteBatch;

	private float logoAlpha;

	private float whiteAlpha;

	private float sinceStarted;

	[ServiceDependency]
	public ITargetRenderingManager TRM { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	public FakeOutro(Game game)
		: base(game)
	{
		base.DrawOrder = 100000;
	}

	protected override void LoadContent()
	{
		HappyLogo = CMProvider.Global.Load<Texture2D>("Other Textures/splash/FEZHAPPY_BW");
		SpriteBatch = new SpriteBatch(base.GraphicsDevice);
		SoundManager.GlobalVolumeFactor = 0f;
	}

	public override void Draw(GameTime gameTime)
	{
		sinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds;
		logoAlpha = ((sinceStarted > 0.5f) ? 1 : 0);
		whiteAlpha = 1f;
		TRM.DrawFullscreen(new Color(0f, 0f, 0f, whiteAlpha));
		Vector2 vector = (new Vector2(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height) / 2f).Round();
		SpriteBatch.BeginPoint();
		SpriteBatch.Draw(HappyLogo, vector - (new Vector2(HappyLogo.Width, HappyLogo.Height) / 2f).Round(), new Color(1f, 1f, 1f, logoAlpha));
		SpriteBatch.End();
		if (whiteAlpha == 0f)
		{
			ServiceHelper.RemoveComponent(this);
		}
	}
}
