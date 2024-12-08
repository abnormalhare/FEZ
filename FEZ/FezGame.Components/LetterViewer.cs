using System;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Localization;

namespace FezGame.Components;

public class LetterViewer : DrawableGameComponent
{
	private enum State
	{
		In,
		Wait,
		Out
	}

	private const float FadeSeconds = 0.25f;

	private static readonly Color TextColor = new Color(72, 66, 52, 255);

	private SoundEffect sLetterAppear;

	private Texture2D letterTexture;

	private GlyphTextRenderer textRenderer;

	private TimeSpan fader;

	private SpriteBatch sb;

	private TimeSpan sinceStarted;

	private readonly string LetterName;

	private string LetterText;

	private int oldLetterCount;

	private State state;

	public bool IsDisposed { get; private set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IFontManager FontManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public LetterViewer(Game game, string letter)
		: base(game)
	{
		base.DrawOrder = 100;
		LetterName = letter;
	}

	protected override void LoadContent()
	{
		sLetterAppear = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/LetterAppear");
		letterTexture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/mail/" + LetterName + "_1");
		textRenderer = new GlyphTextRenderer(base.Game);
		sb = new SpriteBatch(base.GraphicsDevice);
		PlayerManager.CanControl = false;
		PlayerManager.Action = ActionType.ReadTurnAround;
		LetterText = GameText.GetString(LetterName);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		sb.Dispose();
		PlayerManager.CanControl = true;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap)
		{
			return;
		}
		sinceStarted += gameTime.ElapsedGameTime;
		if (state == State.Wait)
		{
			CheckForKeyPress();
		}
		else if (state == State.Out)
		{
			fader -= gameTime.ElapsedGameTime;
		}
		else
		{
			fader += gameTime.ElapsedGameTime;
			CheckForKeyPress();
		}
		if (fader.TotalSeconds > 0.25 || fader.TotalSeconds < 0.0)
		{
			switch (state)
			{
			case State.In:
				state = State.Wait;
				break;
			case State.Out:
				IsDisposed = true;
				ServiceHelper.RemoveComponent(this);
				break;
			}
		}
	}

	private void CheckForKeyPress()
	{
		if (InputManager.Jump == FezButtonState.Pressed || InputManager.CancelTalk == FezButtonState.Pressed)
		{
			if (sinceStarted.TotalSeconds < 15.0)
			{
				sinceStarted = TimeSpan.FromSeconds(15.0);
			}
			else
			{
				state = State.Out;
			}
			fader = TimeSpan.FromSeconds(0.25);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		float num = Easing.EaseOut(FezMath.Saturate(fader.TotalSeconds / 0.25), EasingType.Sine);
		float num2 = base.GraphicsDevice.Viewport.Width;
		float num3 = base.GraphicsDevice.Viewport.Height;
		float num4 = letterTexture.Width;
		float num5 = letterTexture.Height;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		float viewScale = base.GraphicsDevice.GetViewScale();
		_ = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		float num6 = (float)base.GraphicsDevice.Viewport.Height / (720f * viewScale);
		Matrix textureMatrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -0.5f, -0.5f, 1f, 0f, 0f, 0f, 0f, 0f) * new Matrix(num2 / num4 / 4f / viewScale, 0f, 0f, 0f, 0f, num3 / num5 / 4f / viewScale, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f) * new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0.85f, 0.1f + 0.4f * num6, 1f, 0f, 0f, 0f, 0f, 0f);
		TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, 0.4f * num));
		TargetRenderer.DrawFullscreen(letterTexture, textureMatrix, new Color(1f, 1f, 1f, num));
		sb.BeginPoint();
		SpriteFont spriteFont = (Culture.IsCJK ? FontManager.Small : FontManager.Big);
		float num7 = (Culture.IsCJK ? (FontManager.SmallFactor + 0.05f) : FontManager.BigFactor);
		num7 *= viewScale;
		string letterText = LetterText;
		int num8 = (Culture.IsCJK ? 500 : 135);
		letterText = WordWrap.Split(letterText, spriteFont, num8);
		int lineSpacing = spriteFont.LineSpacing;
		if (!Culture.IsCJK)
		{
			spriteFont.LineSpacing = 14;
		}
		string text = letterText.Substring(0, Math.Min(letterText.Length, (int)(sinceStarted.TotalSeconds * 15.0)));
		if (oldLetterCount != CountChars(text))
		{
			sLetterAppear.Emit();
		}
		oldLetterCount = CountChars(text);
		float x = (float)base.GraphicsDevice.Viewport.Width / 2f - 305f * viewScale;
		float num9 = 176f * viewScale;
		Vector3 vector = TextColor.ToVector3();
		textRenderer.DrawString(sb, spriteFont, text, new Vector2(x, num9 * num6 + num7 * FontManager.TopSpacing), new Color(vector.X, vector.Y, vector.Z, num), num7);
		if (!Culture.IsCJK)
		{
			spriteFont.LineSpacing = lineSpacing;
		}
		float alpha = num * (float)FezMath.Saturate(sinceStarted.TotalSeconds - 2.0);
		x = (float)base.GraphicsDevice.Viewport.Width / 2f - 330f * viewScale;
		textRenderer.DrawShadowedText(sb, FontManager.Big, StaticText.GetString("AchievementInTrialResume"), new Vector2(x, num6 * 115f * viewScale + FontManager.TopSpacing * num7), new Color(0.5f, 1f, 0.5f, alpha), FontManager.BigFactor * viewScale);
		sb.End();
	}

	private int CountChars(string text)
	{
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsWhiteSpace(text[i]))
			{
				num++;
			}
		}
		return num;
	}
}
