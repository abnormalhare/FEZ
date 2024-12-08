using System;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class AchievementSellScreen : DrawableGameComponent
{
	private static readonly TimeSpan TransitionDuration = TimeSpan.FromSeconds(0.5);

	private readonly SteamAchievement achievement;

	private Texture2D black;

	private SpriteBatch SpriteBatch;

	private TimeSpan sinceTransition;

	private Action nextAction;

	private GlyphTextRenderer tr;

	private bool fadeInComplete;

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IFontManager Fonts { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public AchievementSellScreen(Game game, SteamAchievement achievement)
		: base(game)
	{
		this.achievement = achievement;
		base.UpdateOrder = -1;
		base.DrawOrder = 1002;
	}

	public override void Initialize()
	{
		GameState.InCutscene = true;
		GameState.DynamicUpgrade += BackToGame;
		base.Initialize();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		GameState.DynamicUpgrade -= BackToGame;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		SpriteBatch = new SpriteBatch(base.GraphicsDevice);
		tr = new GlyphTextRenderer(base.Game);
		black = CMProvider.Global.Load<Texture2D>("Other Textures/FullBlack");
	}

	public override void Update(GameTime gameTime)
	{
		if (InputManager.GrabThrow == FezButtonState.Pressed)
		{
			UnlockFullGame();
		}
		if (InputManager.Jump == FezButtonState.Pressed)
		{
			sinceTransition = TimeSpan.Zero;
			nextAction = BackToGame;
		}
		if (!(sinceTransition < TransitionDuration))
		{
			return;
		}
		sinceTransition += gameTime.ElapsedGameTime;
		if (sinceTransition >= TransitionDuration)
		{
			if (!fadeInComplete)
			{
				fadeInComplete = true;
			}
			else
			{
				nextAction();
			}
		}
	}

	private void BackToGame()
	{
		GameState.InCutscene = false;
		SoundManager.Resume();
		ServiceHelper.RemoveComponent(this);
	}

	private void UnlockFullGame()
	{
	}

	public override void Draw(GameTime gameTime)
	{
		float num = FezMath.Saturate((float)sinceTransition.Ticks / (float)TransitionDuration.Ticks);
		Vector2 vector = new Vector2(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height);
		float num2 = ((!fadeInComplete) ? num : ((nextAction != null) ? (1f - num) : 1f));
		TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, num2 * 0.25f));
		SpriteBatch.BeginPoint();
		SpriteBatch.Draw(black, new Rectangle(0, (int)(vector.Y / 2f - vector.Y / 4f), (int)vector.X, (int)(vector.Y / 2f)), new Color(0f, 0f, 0f, num2 * 0.9f));
		tr.DrawCenteredString(SpriteBatch, Fonts.Big, StaticText.GetString("AchievementInTrialTitle"), new Color(1f, 1f, 1f, num2), new Vector2(0f, vector.Y / 2f - 100f), Fonts.BigFactor);
		tr.DrawCenteredString(SpriteBatch, Fonts.Small, StaticText.GetString("AchievementInTrialText"), new Color(1f, 1f, 1f, num2), new Vector2(0f, vector.Y / 2f), Fonts.SmallFactor);
		tr.DrawCenteredString(SpriteBatch, Fonts.Small, StaticText.GetString("AchievementInTrialSellText"), new Color(1f, 1f, 1f, num2), new Vector2(0f, vector.Y / 2f + 40f), Fonts.SmallFactor);
		float num3 = Fonts.Small.MeasureString(StaticText.GetString("AchievementInTrialResume")).X * Fonts.SmallFactor;
		tr.DrawShadowedText(SpriteBatch, Fonts.Small, StaticText.GetString("AchievementInTrialResume"), new Vector2(vector.X - tr.Margin.X - num3, vector.Y / 2f + 125f), new Color(0.5f, 1f, 0.5f, num2), Fonts.SmallFactor);
		SpriteBatch.End();
	}
}
