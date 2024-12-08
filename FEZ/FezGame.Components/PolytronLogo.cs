using System;
using System.Linq;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class PolytronLogo : DrawableGameComponent
{
	private const int Detail = 100;

	private const float StripThickness = 0.091f;

	private const float Zoom = 0.7f;

	private static readonly Color[] StripColors = new Color[4]
	{
		new Color(0, 170, 255),
		new Color(255, 255, 0),
		new Color(255, 106, 0),
		new Color(0, 0, 0)
	};

	private Mesh LogoMesh;

	private Texture2D PolytronText;

	private SoundEffect sPolytron;

	private SoundEmitter iPolytron;

	private SpriteBatch spriteBatch;

	private float SinceStarted;

	public float Opacity { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public PolytronLogo(Game game)
		: base(game)
	{
		base.Visible = false;
		base.Enabled = false;
	}

	public override void Initialize()
	{
		base.Initialize();
		LogoMesh = new Mesh
		{
			AlwaysOnTop = true
		};
		for (int i = 0; i < 4; i++)
		{
			FezVertexPositionColor[] array = new FezVertexPositionColor[202];
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = new FezVertexPositionColor(Vector3.Zero, StripColors[i]);
			}
			LogoMesh.AddGroup().Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(array, Enumerable.Range(0, array.Length).ToArray(), PrimitiveType.TriangleStrip);
		}
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		float num2 = (float)base.GraphicsDevice.Viewport.Height / (720f * viewScale);
		int width = base.GraphicsDevice.Viewport.Width;
		int height = base.GraphicsDevice.Viewport.Height;
		LogoMesh.Position = new Vector3(-0.1975f / num, -0.25f / num2, 0f);
		LogoMesh.Scale = new Vector3(new Vector2(500f) * viewScale / new Vector2(width, height), 1f);
		bool is1440 = viewScale >= 1.5f;
		sPolytron = CMProvider.Get(CM.Intro).Load<SoundEffect>("Sounds/Intro/PolytronJingle");
		DrawActionScheduler.Schedule(delegate
		{
			PolytronText = CMProvider.Get(CM.Intro).Load<Texture2D>("Other Textures/splash/polytron" + (is1440 ? "_1440" : ""));
			spriteBatch = new SpriteBatch(base.GraphicsDevice);
			LogoMesh.Effect = new DefaultEffect.VertexColored
			{
				ForcedProjectionMatrix = Matrix.CreateOrthographic(1.4285715f, 1.4285715f, 0.1f, 100f),
				ForcedViewMatrix = Matrix.CreateLookAt(Vector3.UnitZ, -Vector3.UnitZ, Vector3.Up)
			};
		});
	}

	protected override void Dispose(bool disposing)
	{
		LogoMesh.Dispose();
		spriteBatch.Dispose();
	}

	private void UpdateStripe(int i, float step)
	{
		FezVertexPositionColor[] vertices = (LogoMesh.Groups[i].Geometry as IndexedUserPrimitives<FezVertexPositionColor>).Vertices;
		Vector3 position = Vector3.Zero;
		Vector3 position2 = Vector3.Zero;
		for (int j = 0; j <= 100; j++)
		{
			if (j < 20)
			{
				float num = (float)j / 20f * FezMath.Saturate(step / 0.2f);
				Vector3 vector2 = (vertices[j * 2].Position = new Vector3((float)(i + 1) * 0.091f, num * 0.5f, 0f));
				position = vector2;
				vector2 = (vertices[j * 2 + 1].Position = new Vector3((float)i * 0.091f, num * 0.5f, 0f));
				position2 = vector2;
			}
			else if (j > 80 && step > 0.8f)
			{
				float num2 = ((float)j - 80f) / 20f * FezMath.Saturate((step - 0.8f) / 0.2f / 0.272f);
				Vector3 vector2 = (vertices[j * 2].Position = new Vector3(0.5f - num2 * 0.136f, (float)(i + 1) * 0.091f, 0f));
				position = vector2;
				vector2 = (vertices[j * 2 + 1].Position = new Vector3(0.5f - num2 * 0.136f, (float)i * 0.091f, 0f));
				position2 = vector2;
			}
			else if (j >= 20 && j <= 80 && step > 0.2f)
			{
				float num3 = ((float)j - 20f) / 60f * FezMath.Saturate((step - 0.2f) / 0.6f) * ((float)Math.PI / 2f) * 3f - (float)Math.PI / 2f;
				Vector3 vector2 = (vertices[j * 2].Position = new Vector3((float)Math.Sin(num3) * (0.5f - (float)(i + 1) * 0.091f) + 0.5f, (float)Math.Cos(num3) * (0.5f - (float)(i + 1) * 0.091f) + 0.5f, 0f));
				position = vector2;
				vector2 = (vertices[j * 2 + 1].Position = new Vector3((float)Math.Sin(num3) * (0.5f - (float)i * 0.091f) + 0.5f, (float)Math.Cos(num3) * (0.5f - (float)i * 0.091f) + 0.5f, 0f));
				position2 = vector2;
			}
			else
			{
				vertices[j * 2].Position = position;
				vertices[j * 2 + 1].Position = position2;
			}
		}
	}

	public void End()
	{
		if (!iPolytron.Dead)
		{
			iPolytron.FadeOutAndDie(0.1f);
		}
		iPolytron = null;
	}

	public override void Update(GameTime gameTime)
	{
		if (SinceStarted == 0f && gameTime.ElapsedGameTime.Ticks != 0L && sPolytron != null)
		{
			iPolytron = sPolytron.Emit();
			sPolytron = null;
		}
		SinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds;
		float num = FezMath.Saturate(SinceStarted / 1.75f);
		UpdateStripe(3, Easing.EaseOut(Easing.EaseIn(num, EasingType.Quadratic), EasingType.Quartic) * 0.86f);
		UpdateStripe(2, Easing.EaseOut(Easing.EaseIn(num, EasingType.Cubic), EasingType.Quartic) * 0.86f);
		UpdateStripe(1, Easing.EaseOut(Easing.EaseIn(num, EasingType.Quartic), EasingType.Quartic) * 0.86f);
		UpdateStripe(0, Easing.EaseOut(Easing.EaseIn(num, EasingType.Quintic), EasingType.Quartic) * 0.86f);
	}

	public override void Draw(GameTime gameTime)
	{
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		Vector2 vector = (new Vector2(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height) / 2f).Round();
		float viewScale = base.GraphicsDevice.GetViewScale();
		LogoMesh.Material.Opacity = Opacity;
		LogoMesh.Draw();
		spriteBatch.Begin();
		float num = Easing.EaseOut(FezMath.Saturate((SinceStarted - 1.5f) / 0.25f), EasingType.Quadratic);
		spriteBatch.Draw(PolytronText, vector + new Vector2((float)(-PolytronText.Width) / 2f, 120f * viewScale).Round(), new Color(1f, 1f, 1f, Opacity * num));
		spriteBatch.End();
	}
}
