using System;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class FezLogo : DrawableGameComponent
{
	private Mesh LogoMesh;

	private Mesh WireMesh;

	private DefaultEffect FezEffect;

	public StarField Starfield;

	public float SinceStarted;

	private bool inverted;

	public Mesh InLogoMesh;

	private SoundEffect sGlitch1;

	private SoundEffect sGlitch2;

	private SoundEffect sGlitch3;

	public bool IsDisposed;

	private float untilGlitch;

	private int forFrames;

	private float[] glitchTilt = new float[3];

	private Vector3[] glitchScale = new Vector3[3];

	private float[] glitchOpacity = new float[3];

	public float Zoom;

	public bool Inverted
	{
		get
		{
			return inverted;
		}
		set
		{
			inverted = value;
			base.Enabled = true;
			SinceStarted = (inverted ? 6 : 0);
		}
	}

	public bool Glitched { get; set; }

	public bool DoubleTime { get; set; }

	public bool HalfSpeed { get; set; }

	public bool IsFullscreen { get; set; }

	public Texture LogoTexture { get; set; }

	public float LogoTextureXFade { get; set; }

	public float Opacity
	{
		set
		{
			LogoMesh.Material.Opacity = (WireMesh.Material.Opacity = value);
			Starfield.Opacity = value;
		}
	}

	public bool TransitionStarted { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public FezLogo(Game game)
		: base(game)
	{
		base.Visible = false;
		base.DrawOrder = 2006;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		LogoMesh.Dispose();
		WireMesh.Dispose();
		IsDisposed = true;
	}

	public override void Initialize()
	{
		base.Initialize();
		LogoMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Alphablending
		};
		WireMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true
		};
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(0f, 0f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(0f, 1f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(0f, 2f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(1f, 2f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(0f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(1f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(2f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(0f, 0f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(1f, 0f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(2f, 0f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(0f, 1f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(0f, 2f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(1f, 2f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(0f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(1f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(4f, 0f, 0f) + new Vector3(2f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(0f, 0f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(1f, 0f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(2f, 0f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(0f, 1f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(1f, 1f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(2f, 2f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(0f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(1f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		LogoMesh.AddColoredBox(Vector3.One, new Vector3(8f, 0f, 0f) + new Vector3(2f, 3f, 0f), Color.Black, centeredOnOrigin: false);
		IndexedUserPrimitives<FezVertexPositionColor> indexedUserPrimitives = (IndexedUserPrimitives<FezVertexPositionColor>)(WireMesh.AddGroup().Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(PrimitiveType.LineList));
		indexedUserPrimitives.Vertices = new FezVertexPositionColor[16]
		{
			new FezVertexPositionColor(new Vector3(0f, 0f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(1f, 0f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(1f, 2f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(2f, 2f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(2f, 3f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(3f, 3f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(3f, 4f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(0f, 4f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(0f, 0f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(1f, 0f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(1f, 2f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(2f, 2f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(2f, 3f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(3f, 3f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(3f, 4f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(0f, 4f, 1f), Color.White)
		};
		indexedUserPrimitives.Indices = new int[50]
		{
			0, 1, 1, 2, 2, 3, 3, 4, 4, 5,
			5, 6, 6, 7, 7, 0, 8, 9, 9, 10,
			10, 11, 11, 12, 12, 13, 13, 14, 14, 15,
			15, 8, 0, 8, 1, 9, 2, 10, 3, 11,
			4, 12, 5, 13, 6, 14, 7, 15, 0, 8
		};
		indexedUserPrimitives = (IndexedUserPrimitives<FezVertexPositionColor>)(WireMesh.AddGroup().Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(PrimitiveType.LineList));
		indexedUserPrimitives.Vertices = new FezVertexPositionColor[20]
		{
			new FezVertexPositionColor(new Vector3(4f, 0f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 0f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 1f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(5f, 1f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(5f, 2f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(6f, 2f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(6f, 3f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 3f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 4f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(4f, 4f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(4f, 0f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 0f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 1f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(5f, 1f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(5f, 2f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(6f, 2f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(6f, 3f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 3f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(7f, 4f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(4f, 4f, 1f), Color.White)
		};
		indexedUserPrimitives.Indices = new int[60]
		{
			0, 1, 1, 2, 2, 3, 3, 4, 4, 5,
			5, 6, 6, 7, 7, 8, 8, 9, 9, 0,
			10, 11, 11, 12, 12, 13, 13, 14, 14, 15,
			15, 16, 16, 17, 17, 18, 18, 19, 19, 10,
			0, 10, 1, 11, 2, 12, 3, 13, 4, 14,
			5, 15, 6, 16, 7, 17, 8, 18, 9, 19
		};
		indexedUserPrimitives = (IndexedUserPrimitives<FezVertexPositionColor>)(WireMesh.AddGroup().Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(PrimitiveType.LineList));
		indexedUserPrimitives.Vertices = new FezVertexPositionColor[22]
		{
			new FezVertexPositionColor(new Vector3(8f, 0f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 0f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 1f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(10f, 1f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(10f, 2f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 2f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 4f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(8f, 4f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(8f, 3f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(10f, 3f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(8f, 2f, 0f), Color.White),
			new FezVertexPositionColor(new Vector3(8f, 0f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 0f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 1f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(10f, 1f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(10f, 2f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 2f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(11f, 4f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(8f, 4f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(8f, 3f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(10f, 3f, 1f), Color.White),
			new FezVertexPositionColor(new Vector3(8f, 2f, 1f), Color.White)
		};
		indexedUserPrimitives.Indices = new int[70]
		{
			0, 1, 1, 2, 2, 3, 3, 4, 4, 5,
			5, 6, 6, 7, 7, 8, 8, 9, 9, 4,
			4, 10, 10, 0, 11, 12, 12, 13, 13, 14,
			14, 15, 15, 16, 16, 17, 17, 18, 18, 19,
			19, 20, 20, 15, 15, 21, 21, 11, 0, 11,
			1, 12, 2, 13, 3, 14, 4, 15, 5, 16,
			6, 17, 7, 18, 8, 19, 9, 20, 10, 21
		};
		Mesh wireMesh = WireMesh;
		Vector3 position = (LogoMesh.Position = new Vector3(-5.5f, -2f, -0.5f));
		wireMesh.Position = position;
		WireMesh.BakeTransform<FezVertexPositionColor>();
		LogoMesh.BakeTransform<FezVertexPositionColor>();
		DrawActionScheduler.Schedule(delegate
		{
			Mesh wireMesh2 = WireMesh;
			BaseEffect effect = (LogoMesh.Effect = (FezEffect = new DefaultEffect.VertexColored()));
			wireMesh2.Effect = effect;
		});
		ContentManager contentManager = CMProvider.Get(CM.Menu);
		sGlitch1 = contentManager.Load<SoundEffect>("Sounds/Intro/FezLogoGlitch1");
		sGlitch2 = contentManager.Load<SoundEffect>("Sounds/Intro/FezLogoGlitch2");
		sGlitch3 = contentManager.Load<SoundEffect>("Sounds/Intro/FezLogoGlitch3");
		ServiceHelper.AddComponent(Starfield = new StarField(base.Game));
		Starfield.Opacity = 0f;
		LogoMesh.Material.Opacity = 0f;
		untilGlitch = RandomHelper.Between(0.3333333432674408, 1.0);
	}

	public override void Update(GameTime gameTime)
	{
		if (IsDisposed || GameState.Paused)
		{
			return;
		}
		if (TransitionStarted)
		{
			SinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds * (Inverted ? (-0.1f) : (DoubleTime ? 2f : (HalfSpeed ? 0.75f : 1f)));
		}
		float num = (Zoom = Math.Min(Easing.EaseIn(FezMath.Saturate(SinceStarted / 5f), EasingType.Sine), 0.999f));
		if (LogoMesh.Material.Opacity == 1f && Glitched && num <= 0.75f)
		{
			untilGlitch -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (untilGlitch <= 0f)
			{
				untilGlitch = RandomHelper.Between(0.3333333432674408, 2.0);
				glitchTilt[0] = RandomHelper.Between(0.0, 1.0);
				glitchTilt[1] = RandomHelper.Between(0.0, 1.0);
				glitchTilt[2] = RandomHelper.Between(0.0, 1.0);
				glitchOpacity[0] = RandomHelper.Between(0.25, 1.0);
				glitchOpacity[1] = RandomHelper.Between(0.25, 1.0);
				glitchOpacity[2] = RandomHelper.Between(0.25, 1.0);
				glitchScale[0] = new Vector3(RandomHelper.Between(0.75, 1.5), RandomHelper.Between(0.75, 1.5), RandomHelper.Between(0.75, 1.5));
				glitchScale[1] = new Vector3(RandomHelper.Between(0.75, 1.5), RandomHelper.Between(0.75, 1.5), RandomHelper.Between(0.75, 1.5));
				glitchScale[2] = new Vector3(RandomHelper.Between(0.75, 1.5), RandomHelper.Between(0.75, 1.5), RandomHelper.Between(0.75, 1.5));
				forFrames = RandomHelper.Random.Next(1, 7);
				if (RandomHelper.Probability(0.3333333432674408))
				{
					sGlitch1.Emit();
				}
				if (RandomHelper.Probability(0.5))
				{
					sGlitch2.Emit();
				}
				else
				{
					sGlitch3.Emit();
				}
			}
		}
		float aspectRatio = base.GraphicsDevice.Viewport.AspectRatio;
		Mesh wireMesh = WireMesh;
		Vector3 position = (LogoMesh.Position = new Vector3(0f, 0f - num, 0f));
		wireMesh.Position = position;
		if (FezEffect != null)
		{
			FezEffect.ForcedProjectionMatrix = Matrix.CreateOrthographic(10f * aspectRatio * (1f - num), 10f * (1f - num), 0.1f, 100f);
		}
		if (Starfield != null)
		{
			Starfield.AdditionalZoom = Easing.EaseInOut(num, EasingType.Quadratic);
			Starfield.HasZoomed = true;
		}
		if (!Inverted && num >= 0.999f)
		{
			IsFullscreen = true;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		DoDraw();
	}

	private void DoDraw()
	{
		if (!IsDisposed)
		{
			if (Fez.LongScreenshot)
			{
				TargetRenderer.DrawFullscreen(Color.White);
			}
			if (forFrames == 0)
			{
				float amount = 1f - Easing.EaseInOut(FezMath.Saturate(SinceStarted / 5f), EasingType.Sine);
				Mesh wireMesh = WireMesh;
				Vector3 scale = (LogoMesh.Scale = new Vector3(1f, 1f, 1f));
				wireMesh.Scale = scale;
				FezEffect.ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Lerp(new Vector3(0f, 0f, 10f), new Vector3(-10f, 10f, 10f), amount), Vector3.Zero, Vector3.Up);
				DrawMaskedLogo();
			}
			else
			{
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.Red | ColorWriteChannels.Green);
				LogoMesh.Scale = glitchScale[0];
				LogoMesh.Material.Opacity = glitchOpacity[0];
				FezEffect.ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Lerp(new Vector3(0f, 0f, 10f), new Vector3(-10f, 10f, 10f), glitchTilt[0]), Vector3.Zero, Vector3.Up);
				DrawMaskedLogo();
				base.GraphicsDevice.Clear(ClearOptions.Stencil, Color.Black, 0f, 0);
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.Green | ColorWriteChannels.Blue);
				LogoMesh.Scale = glitchScale[1];
				LogoMesh.Material.Opacity = glitchOpacity[1];
				FezEffect.ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Lerp(new Vector3(0f, 0f, 10f), new Vector3(-10f, 10f, 10f), glitchTilt[1]), Vector3.Zero, Vector3.Up);
				DrawMaskedLogo();
				base.GraphicsDevice.Clear(ClearOptions.Stencil, Color.Black, 0f, 0);
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.Red | ColorWriteChannels.Blue);
				LogoMesh.Scale = glitchScale[2];
				LogoMesh.Material.Opacity = glitchOpacity[2];
				FezEffect.ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Lerp(new Vector3(0f, 0f, 10f), new Vector3(-10f, 10f, 10f), glitchTilt[2]), Vector3.Zero, Vector3.Up);
				DrawMaskedLogo();
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
				LogoMesh.Material.Opacity = 1f;
				forFrames--;
			}
			if (LogoTexture != null)
			{
				TargetRenderer.DrawFullscreen(LogoTexture, new Color(1f, 1f, 1f, LogoTextureXFade));
				Starfield.Opacity = 1f - LogoTextureXFade;
			}
			if (InLogoMesh != null)
			{
				InLogoMesh.Draw();
			}
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			WireMesh.Draw();
		}
	}

	private void DrawMaskedLogo()
	{
		base.GraphicsDevice.PrepareStencilReadWrite(CompareFunction.NotEqual, StencilMask.BlackHoles);
		LogoMesh.Draw();
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.BlackHoles);
		Starfield.Draw();
	}
}
