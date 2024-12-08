using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class TextScroll : DrawableGameComponent
{
	private const float OpenCloseDuration = 0.5f;

	private static readonly Color TextColor = Color.Black;

	private readonly bool OnTop;

	private string Text;

	private Vector2 MiddlePartSize;

	private bool Ready;

	private Mesh ScrollMesh;

	private Group LeftPart;

	private Group MiddlePart;

	private Group RightPart;

	private Group TextGroup;

	private RenderTarget2D TextTexture;

	private GlyphTextRenderer GTR;

	private SoundEffect sOpen;

	private SoundEffect sClose;

	private TimeSpan SinceOpen;

	private TimeSpan SinceClose;

	public string Key { get; set; }

	public float? Timeout { get; set; }

	public bool Closing { get; set; }

	public TextScroll NextScroll { get; set; }

	[ServiceDependency]
	public IFontManager FontManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public static void PreInitialize()
	{
		IContentManagerProvider cmProvider = ServiceHelper.Get<IContentManagerProvider>();
		DrawActionScheduler.Schedule(delegate
		{
			cmProvider.Global.Load<Texture2D>("Other Textures/SCROLL/SCROLL_A");
			cmProvider.Global.Load<Texture2D>("Other Textures/SCROLL/SCROLL_B");
			cmProvider.Global.Load<Texture2D>("Other Textures/SCROLL/SCROLL_C");
		});
	}

	public TextScroll(Game game, string text, bool onTop)
		: base(game)
	{
		base.DrawOrder = 1001;
		OnTop = onTop;
		Text = text;
	}

	public void Close()
	{
		if (!Closing)
		{
			sClose.Emit();
			Closing = true;
			SinceClose = TimeSpan.Zero;
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (base.GraphicsDevice != null)
		{
			base.GraphicsDevice.DeviceReset -= UpdateViewScale;
		}
		if (TextTexture != null)
		{
			TextTexture.Dispose();
			TextTexture = null;
		}
		if (ScrollMesh != null)
		{
			ScrollMesh.Dispose();
			ScrollMesh = null;
		}
		if (GameState != null)
		{
			GameState.ActiveScroll = NextScroll;
			if (NextScroll != null)
			{
				ServiceHelper.AddComponent(GameState.ActiveScroll);
			}
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		sOpen = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ScrollOpen");
		sClose = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ScrollClose");
		sOpen.Emit();
	}

	private void LateLoadContent()
	{
		GTR = new GlyphTextRenderer(base.Game);
		SpriteFont big = FontManager.Big;
		float scale = (Culture.IsCJK ? 0.6f : 1f);
		Vector2 vector = GTR.MeasureWithGlyphs(big, Text, scale);
		MiddlePartSize = vector * new Vector2(1f, 1.25f);
		int width = (int)MiddlePartSize.X;
		int height = (int)MiddlePartSize.Y;
		Vector2 middlePartSize = MiddlePartSize;
		if (TextTexture != null)
		{
			TextTexture.Dispose();
			TextTexture = null;
		}
		TextTexture = new RenderTarget2D(base.GraphicsDevice, width, height, mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, base.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		using (SpriteBatch spriteBatch = new SpriteBatch(base.GraphicsDevice))
		{
			base.GraphicsDevice.SetRenderTarget(TextTexture);
			base.GraphicsDevice.PrepareDraw();
			base.GraphicsDevice.Clear(ClearOptions.Target, new Color(215, 188, 122, 0), 1f, 0);
			spriteBatch.BeginPoint();
			GTR.DrawString(spriteBatch, big, Text, middlePartSize / 2f - vector / 2f + new Vector2(0f, FontManager.TopSpacing / 2f), TextColor, scale);
			spriteBatch.End();
			base.GraphicsDevice.SetRenderTarget(null);
		}
		MiddlePartSize /= (Culture.IsCJK ? 3f : 2f);
		MiddlePartSize -= new Vector2(16f, 0f);
		ScrollMesh = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			Effect = new DefaultEffect.Textured(),
			AlwaysOnTop = true,
			DepthWrites = false,
			Material = 
			{
				Opacity = 0f
			},
			SamplerState = SamplerState.PointClamp
		};
		LeftPart = ScrollMesh.AddFace(new Vector3(2f), new Vector3(0f, 0.5f, 0f), FaceOrientation.Front, centeredOnOrigin: true);
		LeftPart.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/SCROLL/SCROLL_A");
		MiddlePart = ScrollMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		MiddlePart.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/SCROLL/SCROLL_B");
		RightPart = ScrollMesh.AddFace(new Vector3(2f), new Vector3(0f, -0.5f, 0f), FaceOrientation.Front, centeredOnOrigin: true);
		RightPart.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/SCROLL/SCROLL_C");
		TextGroup = ScrollMesh.AddFace(new Vector3((MiddlePartSize.X + 16f) / 16f, MiddlePartSize.Y / 16f, 1f), new Vector3(-0.125f, 0f, 0f), FaceOrientation.Front, centeredOnOrigin: true);
		TextGroup.SamplerState = (Culture.IsCJK ? SamplerState.AnisotropicClamp : SamplerState.PointClamp);
		TextGroup.Texture = TextTexture;
		TextGroup.Material = new Material
		{
			Opacity = 0f
		};
		ScrollMesh.Effect.ForcedProjectionMatrix = Matrix.Identity;
		ScrollMesh.Effect.ForcedViewMatrix = Matrix.Identity;
		base.GraphicsDevice.DeviceReset += UpdateViewScale;
		if (Culture.IsCJK)
		{
			MiddlePartSize.X /= 2f;
		}
		Ready = true;
	}

	private void UpdateViewScale(object sender, EventArgs e)
	{
		if (!Closing)
		{
			LateLoadContent();
			OpenOrClose(1f);
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		if (!Ready)
		{
			LateLoadContent();
		}
		float num = base.GraphicsDevice.Viewport.Width;
		float num2 = base.GraphicsDevice.Viewport.Height;
		ScrollMesh.Scale = new Vector3(96f / num, 96f / num2, 1f) * base.GraphicsDevice.GetViewScale();
		TextGroup.Scale = Vector3.One;
		if (Culture.IsCJK)
		{
			TextGroup.Scale /= 2f;
		}
		ScrollMesh.Position = new Vector3(0.01f, OnTop ? 0.6125f : (-0.6125f), 0f);
		if (Timeout.HasValue)
		{
			Timeout -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (Timeout.Value <= 0f)
			{
				sClose.Emit();
				Closing = true;
				Timeout = null;
				SinceClose = TimeSpan.Zero;
			}
		}
		if (SinceOpen.TotalSeconds < 0.5)
		{
			SinceOpen += gameTime.ElapsedGameTime;
			float step = Easing.EaseInOut(SinceOpen.TotalSeconds / 0.5, EasingType.Cubic, EasingType.Sine);
			OpenOrClose(step);
		}
		else if (Closing)
		{
			SinceClose += gameTime.ElapsedGameTime;
			float step2 = Easing.EaseInOut(1.0 - SinceClose.TotalSeconds / 0.5, EasingType.Cubic, EasingType.Sine);
			OpenOrClose(step2);
			if (SinceClose.TotalSeconds > 0.5)
			{
				ServiceHelper.RemoveComponent(this);
			}
		}
	}

	private void OpenOrClose(float step)
	{
		ScrollMesh.Material.Opacity = FezMath.Saturate(step / 0.4f);
		LeftPart.Position = Vector3.Lerp(new Vector3(-1f, 0f, 0f), new Vector3((0f - MiddlePartSize.X) / 16f / 2f - 1f, 0f, 0f), step);
		RightPart.Position = Vector3.Lerp(new Vector3(1f, 0f, 0f), new Vector3(MiddlePartSize.X / 16f / 2f + 1f, 0f, 0f), step);
		MiddlePart.Scale = Vector3.Lerp(new Vector3(0f, 1f, 1f), new Vector3((MiddlePartSize.X + 1f) / 16f, 1f, 1f), step);
		MiddlePart.TextureMatrix.Set(new Matrix(MathHelper.Lerp(0f, MiddlePartSize.X / 32f, step), 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f));
		TextGroup.Material.Opacity = Easing.EaseIn(FezMath.Saturate((step - 0.75f) / 0.25f), EasingType.Quadratic);
	}

	public override void Draw(GameTime gameTime)
	{
		if (Ready)
		{
			ScrollMesh.Draw();
		}
	}
}
