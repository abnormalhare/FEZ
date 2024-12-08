using System;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class TileTransition : DrawableGameComponent
{
	private struct TileData
	{
		public float X;

		public float Y;

		public bool B;

		public bool Inverted;

		public bool Vertical;
	}

	private const int TilesWide = 1;

	private static SoundEffect sTransition;

	private static readonly object Mutex = new object();

	private static TileTransition CurrentTransition;

	private RenderTargetHandle textureA;

	private RenderTargetHandle textureB;

	private bool taCaptured;

	private bool tbCaptured;

	private float sinceStarted;

	private Mesh mesh;

	public Action ScreenCaptured { private get; set; }

	public Func<bool> WaitFor { private get; set; }

	public bool IsDisposed { get; private set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public TileTransition(Game game)
		: base(game)
	{
		if (CurrentTransition != null)
		{
			ServiceHelper.RemoveComponent(CurrentTransition);
			CurrentTransition = null;
		}
		CurrentTransition = this;
		base.DrawOrder = 2099;
	}

	public override void Initialize()
	{
		base.Initialize();
		lock (Mutex)
		{
			if (sTransition == null)
			{
				sTransition = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/CubeTransition");
			}
		}
		mesh = new Mesh
		{
			Effect = new DefaultEffect.Textured
			{
				ForcedProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60f), 1f, 0.1f, 100f),
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, -1.365f), new Vector3(0f, 0f, 0f), Vector3.Up)
			},
			DepthWrites = false,
			AlwaysOnTop = true,
			Blending = BlendingMode.Opaque
		};
		for (int i = 0; i < 1; i++)
		{
			for (int j = 0; j < 1; j++)
			{
				float num = (float)i / 1f;
				float num2 = (float)(i + 1) / 1f;
				float num3 = (float)j / 1f;
				float num4 = (float)(j + 1) / 1f;
				bool flag = RandomHelper.Probability(0.5);
				bool flag2 = RandomHelper.Probability(0.5);
				Group group = mesh.AddGroup();
				if (flag2)
				{
					group.Geometry = new IndexedUserPrimitives<VertexPositionTexture>(new VertexPositionTexture[4]
					{
						new VertexPositionTexture(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(1f - num, 1f - num4)),
						new VertexPositionTexture(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1f - num2, 1f - num4)),
						new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1f - num, 1f - num3)),
						new VertexPositionTexture(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1f - num2, 1f - num3))
					}, new int[6] { 0, 2, 1, 2, 3, 1 }, PrimitiveType.TriangleList);
				}
				else
				{
					group.Geometry = new IndexedUserPrimitives<VertexPositionTexture>(new VertexPositionTexture[4]
					{
						new VertexPositionTexture(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(1f - num, 1f - num4)),
						new VertexPositionTexture(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1f - num2, 1f - num4)),
						new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1f - num, 1f - num3)),
						new VertexPositionTexture(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1f - num2, 1f - num3))
					}, new int[6] { 0, 2, 1, 2, 3, 1 }, PrimitiveType.TriangleList);
				}
				group.Scale = new Vector3(1f, 1f, 1f / (float)(flag2 ? 1 : 1));
				group.Position = new Vector3(num, num3, 0f);
				group.CustomData = new TileData
				{
					X = num + RandomHelper.Centered(0.15000000596046448),
					Y = num3 + RandomHelper.Centered(0.15000000596046448),
					B = false,
					Inverted = flag,
					Vertical = flag2
				};
				group.Material = new Material();
				group = mesh.AddGroup();
				if (flag2)
				{
					group.Geometry = new IndexedUserPrimitives<VertexPositionTexture>(new VertexPositionTexture[4]
					{
						new VertexPositionTexture(new Vector3(-0.5f, flag ? 0.5f : (-0.5f), -0.5f), new Vector2(1f - num, flag ? (1f - num3) : (1f - num4))),
						new VertexPositionTexture(new Vector3(0.5f, flag ? 0.5f : (-0.5f), -0.5f), new Vector2(1f - num2, flag ? (1f - num3) : (1f - num4))),
						new VertexPositionTexture(new Vector3(-0.5f, flag ? 0.5f : (-0.5f), 0.5f), new Vector2(1f - num, flag ? (1f - num4) : (1f - num3))),
						new VertexPositionTexture(new Vector3(0.5f, flag ? 0.5f : (-0.5f), 0.5f), new Vector2(1f - num2, flag ? (1f - num4) : (1f - num3)))
					}, (!flag) ? new int[6] { 0, 2, 1, 2, 3, 1 } : new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
				}
				else
				{
					group.Geometry = new IndexedUserPrimitives<VertexPositionTexture>(new VertexPositionTexture[4]
					{
						new VertexPositionTexture(new Vector3(flag ? 0.5f : (-0.5f), 0.5f, 0.5f), new Vector2(flag ? (1f - num2) : (1f - num), 1f - num4)),
						new VertexPositionTexture(new Vector3(flag ? 0.5f : (-0.5f), 0.5f, -0.5f), new Vector2(flag ? (1f - num) : (1f - num2), 1f - num4)),
						new VertexPositionTexture(new Vector3(flag ? 0.5f : (-0.5f), -0.5f, 0.5f), new Vector2(flag ? (1f - num2) : (1f - num), 1f - num3)),
						new VertexPositionTexture(new Vector3(flag ? 0.5f : (-0.5f), -0.5f, -0.5f), new Vector2(flag ? (1f - num) : (1f - num2), 1f - num3))
					}, (!flag) ? new int[6] { 0, 2, 1, 2, 3, 1 } : new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
				}
				group.Scale = new Vector3(1f, 1f, 1f / (float)(flag2 ? 1 : 1));
				group.Position = new Vector3(num, num3, 0f);
				group.CustomData = new TileData
				{
					X = num + RandomHelper.Centered(0.15000000596046448),
					Y = num3 + RandomHelper.Centered(0.15000000596046448),
					B = true,
					Inverted = flag,
					Vertical = flag2
				};
				group.Material = new Material();
			}
		}
		mesh.Position = new Vector3(0f, 0f, 0f);
		textureA = TargetRenderer.TakeTarget();
		textureB = TargetRenderer.TakeTarget();
		foreach (Group group2 in mesh.Groups)
		{
			if (((TileData)group2.CustomData).B)
			{
				group2.Texture = textureB.Target;
			}
			else
			{
				group2.Texture = textureA.Target;
			}
		}
		TargetRenderer.ScheduleHook(base.DrawOrder, textureA.Target);
		sTransition.Emit();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (textureA != null)
		{
			TargetRenderer.ReturnTarget(textureA);
			TargetRenderer.UnscheduleHook(textureA.Target);
		}
		if (textureB != null)
		{
			TargetRenderer.ReturnTarget(textureB);
			TargetRenderer.UnscheduleHook(textureB.Target);
		}
		textureA = (textureB = null);
		mesh.Dispose();
		IsDisposed = true;
		CurrentTransition = null;
	}

	public override void Update(GameTime gameTime)
	{
		if (!tbCaptured)
		{
			return;
		}
		sinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds * 1.5f;
		int num = 0;
		foreach (Group group in mesh.Groups)
		{
			TileData tileData = (TileData)group.CustomData;
			float num2 = Easing.EaseOut(FezMath.Saturate(sinceStarted), EasingType.Quadratic) * ((float)Math.PI / 2f);
			group.Rotation = Quaternion.CreateFromAxisAngle(tileData.Vertical ? Vector3.Left : Vector3.Up, tileData.Inverted ? num2 : (0f - num2));
			group.Material.Diffuse = new Vector3(0.125f) + 0.875f * (tileData.B ? new Vector3((float)Math.Sin(num2)) : new Vector3((float)(1.0 - Math.Sin(num2))));
			if (num2 >= (float)Math.PI / 2f)
			{
				num++;
			}
		}
		if (num == mesh.Groups.Count)
		{
			ServiceHelper.RemoveComponent(this);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!taCaptured && TargetRenderer.IsHooked(textureA.Target))
		{
			TargetRenderer.Resolve(textureA.Target, reschedule: false);
			taCaptured = true;
			if (ScreenCaptured != null)
			{
				ScreenCaptured();
				ScreenCaptured = null;
			}
		}
		if (TargetRenderer.IsHooked(textureB.Target))
		{
			TargetRenderer.Resolve(textureB.Target, reschedule: true);
			tbCaptured = true;
			WaitFor = null;
		}
		if ((WaitFor == null || WaitFor()) && !TargetRenderer.IsHooked(textureB.Target) && !tbCaptured)
		{
			TargetRenderer.ScheduleHook(base.DrawOrder, textureB.Target);
		}
		base.GraphicsDevice.Clear(Color.Black);
		base.GraphicsDevice.SetupViewport();
		mesh.Draw();
	}
}
