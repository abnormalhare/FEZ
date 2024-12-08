using System;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class StarField : DrawableGameComponent
{
	private static readonly Color[] Colors = new Color[11]
	{
		new Color(20, 1, 28),
		new Color(108, 27, 44),
		new Color(225, 125, 53),
		new Color(246, 231, 108),
		new Color(155, 226, 177),
		new Color(67, 246, 255),
		new Color(100, 154, 224),
		new Color(214, 133, 180),
		new Color(189, 63, 117),
		new Color(98, 21, 88),
		new Color(255, 255, 255)
	};

	private static IIndexedPrimitiveCollection StarGeometry;

	private Mesh StarsMesh;

	private Mesh TrailsMesh;

	private FakePointSpritesEffect StarEffect;

	private bool Done;

	public float Opacity = 1f;

	private TimeSpan sinceStarted;

	private Matrix? savedViewMatrix;

	public bool HasHorizontalTrails { get; set; }

	public bool ReverseTiming { get; set; }

	public bool FollowCamera { get; set; }

	public bool HasZoomed { get; set; }

	public float AdditionalZoom { get; set; }

	public float AdditionalScale { get; set; }

	public bool IsDisposed { get; private set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public StarField(Game game)
		: base(game)
	{
		base.DrawOrder = 2006;
		base.Enabled = false;
		base.Visible = false;
	}

	public override void Initialize()
	{
		base.Initialize();
		StarsMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Additive,
			Culling = CullMode.None
		};
		DrawActionScheduler.Schedule(delegate
		{
			Mesh starsMesh = StarsMesh;
			FakePointSpritesEffect obj = new FakePointSpritesEffect
			{
				ForcedProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(75f), CameraManager.AspectRatio, 0.1f, 1000f)
			};
			FakePointSpritesEffect effect = obj;
			StarEffect = obj;
			starsMesh.Effect = effect;
		});
		if (HasHorizontalTrails)
		{
			TrailsMesh = new Mesh
			{
				AlwaysOnTop = true,
				DepthWrites = false,
				Blending = BlendingMode.Additive
			};
			DrawActionScheduler.Schedule(delegate
			{
				TrailsMesh.Effect = new HorizontalTrailsEffect
				{
					ForcedProjectionMatrix = StarEffect.ForcedProjectionMatrix
				};
			});
		}
		AddStars();
		if (FollowCamera)
		{
			return;
		}
		DrawActionScheduler.Schedule(delegate
		{
			StarEffect.ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);
			if (HasHorizontalTrails)
			{
				(TrailsMesh.Effect as HorizontalTrailsEffect).ForcedViewMatrix = StarEffect.ForcedViewMatrix;
			}
		});
	}

	private void AddStars()
	{
		Texture2D texture = CMProvider.Global.Load<Texture2D>("Other Textures/FullWhite");
		if (StarGeometry != null && !HasHorizontalTrails)
		{
			Group group = StarsMesh.AddGroup();
			group.Texture = texture;
			group.Geometry = StarGeometry;
			return;
		}
		Color[] array = null;
		Vector3[] array2 = null;
		float num = 49f;
		float num2 = num;
		Vector3[] array3 = new Vector3[(int)(num2 * num * num2)];
		if (HasHorizontalTrails)
		{
			array = new Color[(int)(num2 * num * num2) * 2];
			array2 = new Vector3[(int)(num2 * num * num2) * 2];
		}
		Random random = RandomHelper.Random;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; (float)i < num2; i++)
		{
			for (int j = 0; (float)j < num; j++)
			{
				for (int k = 0; (float)k < num2; k++)
				{
					Vector3 vector = new Vector3(((float)i - num2 / 2f) * 100f, ((float)j - num / 2f) * 100f, ((float)k - num2 / 2f) * 100f);
					array3[num3++] = vector;
					if (HasHorizontalTrails)
					{
						array2[num4] = vector;
						array2[num4 + 1] = vector;
						byte b = (byte)random.Next(0, 256);
						byte r = (byte)random.Next(0, 256);
						array[num4] = new Color(r, 0, b, 0);
						array[num4 + 1] = new Color(r, 0, b, 255);
						num4 += 2;
					}
				}
			}
		}
		Group group2 = StarsMesh.AddGroup();
		AddPoints(group2, array3, texture, 2f);
		StarGeometry = group2.Geometry;
		if (HasHorizontalTrails)
		{
			TrailsMesh.AddLines(array, array2, buffered: true);
		}
	}

	private static void AddPoints(Group g, Vector3[] pointCenters, Texture texture, float size)
	{
		BufferedIndexedPrimitives<VertexFakePointSprite> bufferedIndexedPrimitives = (BufferedIndexedPrimitives<VertexFakePointSprite>)(g.Geometry = new BufferedIndexedPrimitives<VertexFakePointSprite>(PrimitiveType.TriangleList));
		bufferedIndexedPrimitives.Vertices = new VertexFakePointSprite[pointCenters.Length * 4];
		bufferedIndexedPrimitives.Indices = new int[pointCenters.Length * 6];
		Random random = RandomHelper.Random;
		int maxValue = Colors.Length;
		VertexFakePointSprite[] vertices = bufferedIndexedPrimitives.Vertices;
		int[] indices = bufferedIndexedPrimitives.Indices;
		for (int i = 0; i < pointCenters.Length; i++)
		{
			Color color = Colors[random.Next(0, maxValue)];
			int num = i * 4;
			vertices[num] = new VertexFakePointSprite(pointCenters[i], color, new Vector2(0f, 0f), new Vector2(0f - size, 0f - size));
			vertices[num + 1] = new VertexFakePointSprite(pointCenters[i], color, new Vector2(1f, 0f), new Vector2(size, 0f - size));
			vertices[num + 2] = new VertexFakePointSprite(pointCenters[i], color, new Vector2(1f, 1f), new Vector2(size, size));
			vertices[num + 3] = new VertexFakePointSprite(pointCenters[i], color, new Vector2(0f, 1f), new Vector2(0f - size, size));
			int num2 = i * 6;
			indices[num2] = num;
			indices[num2 + 1] = num + 1;
			indices[num2 + 2] = num + 2;
			indices[num2 + 3] = num;
			indices[num2 + 4] = num + 2;
			indices[num2 + 5] = num + 3;
		}
		bufferedIndexedPrimitives.UpdateBuffers();
		bufferedIndexedPrimitives.CleanUp();
		g.Texture = texture;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (TrailsMesh != null)
		{
			TrailsMesh.Dispose();
		}
		TrailsMesh = null;
		if (StarsMesh != null)
		{
			StarsMesh.Effect.Dispose();
		}
		StarsMesh = null;
		IsDisposed = true;
	}

	public override void Update(GameTime gameTime)
	{
		if (ReverseTiming)
		{
			sinceStarted -= gameTime.ElapsedGameTime;
			sinceStarted -= gameTime.ElapsedGameTime;
		}
		else
		{
			sinceStarted += gameTime.ElapsedGameTime;
		}
		float num = Easing.EaseIn(sinceStarted.TotalSeconds / 3.0, EasingType.Quartic);
		if (HasHorizontalTrails)
		{
			(TrailsMesh.Effect as HorizontalTrailsEffect).Timing = (float)sinceStarted.TotalSeconds;
		}
		if (!FollowCamera)
		{
			AdditionalZoom = (float)HasZoomed.AsNumeric() + num / 3f;
			AdditionalScale = num / 6f;
		}
		if (!HasHorizontalTrails && num > 40f && !Done)
		{
			Done = true;
			ServiceHelper.RemoveComponent(this);
		}
		if (HasHorizontalTrails && sinceStarted <= TimeSpan.Zero)
		{
			base.Enabled = false;
			sinceStarted = TimeSpan.Zero;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		TargetRenderer.DrawFullscreen(Color.Black);
		Draw();
	}

	public void Draw()
	{
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		if (!FollowCamera)
		{
			StarsMesh.Position = AdditionalZoom * Vector3.Forward * 125f - 2400f * Vector3.Forward;
			StarsMesh.Scale = new Vector3(1f + AdditionalScale, 1f + AdditionalScale, 1f);
		}
		else if (!GameState.InFpsMode)
		{
			StarEffect.ForcedViewMatrix = (savedViewMatrix = null);
			StarsMesh.Position = CameraManager.InterpolatedCenter * 0.5f;
			StarsMesh.Scale = new Vector3(112.5f / (CameraManager.Radius / viewScale / num + 40f));
			if (HasHorizontalTrails)
			{
				TrailsMesh.Position = StarsMesh.Position;
				Mesh trailsMesh = TrailsMesh;
				Vector3 scale = (StarsMesh.Scale = new Vector3(65f / (CameraManager.Radius / viewScale / num + 25f)));
				trailsMesh.Scale = scale;
			}
		}
		else if (CameraManager.ProjectionTransition)
		{
			if (CameraManager.Viewpoint != Viewpoint.Perspective)
			{
				StarEffect.ForcedViewMatrix = Matrix.Lerp(CameraManager.View, CameraManager.View, Easing.EaseOut(CameraManager.ViewTransitionStep, EasingType.Quadratic));
			}
			else if (!savedViewMatrix.HasValue)
			{
				savedViewMatrix = CameraManager.View;
			}
		}
		StarsMesh.Material.Opacity = Opacity;
		StarsMesh.Draw();
		if (base.Enabled && HasHorizontalTrails)
		{
			TrailsMesh.Draw();
		}
	}
}
