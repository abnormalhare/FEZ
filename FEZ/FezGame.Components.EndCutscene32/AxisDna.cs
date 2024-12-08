using System;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene32;

internal class AxisDna : DrawableGameComponent
{
	private enum State
	{
		AxisZoom,
		StrandZoom
	}

	private const float AxisZoomDuration = 10f;

	private const float StrandZoomDuration = 7.42f;

	private const int StrandCount = 750;

	private const int PointCount = 100000;

	private readonly EndCutscene32Host Host;

	private float Time;

	private State ActiveState;

	private Mesh FatAxisMesh;

	private Mesh HelixMesh;

	private Mesh NoiseMesh;

	private Texture2D PurpleGradientTexture;

	private ShimmeringPointsEffect ShimmeringEffect;

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public AxisDna(Game game, EndCutscene32Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		Reset();
	}

	private void Reset()
	{
		FatAxisMesh = new Mesh();
		FatAxisMesh.AddColoredBox(new Vector3(1f, 1f, 10000f) / 200f, new Vector3(-0.5f, -0.5f, 0f) / 200f, Color.Blue, centeredOnOrigin: false);
		FatAxisMesh.Rotation = new Quaternion(-0.7408407f, -0.4897192f, 0.4504161f, 0.09191696f);
		FatAxisMesh.Scale = new Vector3(3f);
		FatAxisMesh.Position = new Vector3(7.574002f, 3.049632f, 5.773395f);
		if (HelixMesh != null && HelixMesh.Groups.Count == 1)
		{
			(HelixMesh.FirstGroup.Geometry as BufferedIndexedPrimitives<FezVertexPositionColor>).Dispose();
		}
		HelixMesh = new Mesh();
		for (int i = 0; i < 750; i++)
		{
			float angle = (float)i / 1f;
			float num = (1f - (float)Math.Pow(((float)i - 375f) / 375f, 2.0)) * 0.2f;
			Group group = HelixMesh.AddColoredBox(new Vector3(num, 70f, num) / 10000f, Vector3.Zero, EndCutscene32Host.PurpleBlack, centeredOnOrigin: true);
			group.Position = new Vector3(0f, 0f, ((float)i - 375f) / 3000f);
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);
		}
		HelixMesh.CollapseToBuffer<FezVertexPositionColor>();
		HelixMesh.Rotation = FatAxisMesh.Rotation;
		HelixMesh.Scale = FatAxisMesh.Scale;
		HelixMesh.Culling = CullMode.None;
		Random random = RandomHelper.Random;
		FezVertexPositionColor[] array = new FezVertexPositionColor[200000];
		int[] array2 = new int[200000];
		for (int j = 0; j < 100000; j++)
		{
			array[j * 2] = new FezVertexPositionColor(new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, 0f), new Color((byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256), 0));
			array[j * 2 + 1] = new FezVertexPositionColor(array[j * 2].Position, new Color(array[j * 2].Color.R, array[j * 2].Color.G, array[j * 2].Color.B, 255));
			array2[j * 2] = j * 2;
			array2[j * 2 + 1] = j * 2 + 1;
		}
		NoiseMesh = new Mesh();
		Group group2 = NoiseMesh.AddGroup();
		BufferedIndexedPrimitives<FezVertexPositionColor> bufferedIndexedPrimitives = new BufferedIndexedPrimitives<FezVertexPositionColor>(array, array2, PrimitiveType.LineList);
		bufferedIndexedPrimitives.UpdateBuffers();
		bufferedIndexedPrimitives.CleanUp();
		group2.Geometry = bufferedIndexedPrimitives;
		DrawActionScheduler.Schedule(delegate
		{
			FatAxisMesh.Effect = new DefaultEffect.VertexColored();
			HelixMesh.Effect = new DefaultEffect.VertexColored();
			NoiseMesh.Effect = (ShimmeringEffect = new ShimmeringPointsEffect());
			NoiseMesh.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(1f, 1f, 0.1f, 100f);
			NoiseMesh.Effect.ForcedViewMatrix = Matrix.CreateLookAt(-Vector3.UnitZ, Vector3.Zero, Vector3.Up);
			PurpleGradientTexture = CMProvider.Global.Load<Texture2D>("Other Textures/end_cutscene/purple_gradient");
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (HelixMesh != null)
		{
			HelixMesh.Dispose();
		}
		if (FatAxisMesh != null)
		{
			FatAxisMesh.Dispose();
		}
		HelixMesh = (FatAxisMesh = (NoiseMesh = null));
		ShimmeringEffect = null;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		Time += num;
		float num2 = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		switch (ActiveState)
		{
		case State.AxisZoom:
		{
			if (Time == 0f)
			{
				CameraManager.Center = Vector3.Zero;
				CameraManager.Direction = Vector3.UnitZ;
				CameraManager.Radius = 10f * base.GraphicsDevice.GetViewScale() * num2;
				CameraManager.SnapInterpolation();
				LevelManager.ActualAmbient = new Color(0.25f, 0.25f, 0.25f);
				LevelManager.ActualDiffuse = Color.White;
				NoiseMesh.Scale = Vector3.One;
			}
			float num4 = FezMath.Saturate(Time / 10f);
			Vector3 vector = Vector3.Transform(Vector3.UnitZ, HelixMesh.Rotation);
			Vector3 direction = FezMath.Slerp(Vector3.UnitZ, -vector, num4 * 0.5f);
			CameraManager.Direction = direction;
			FatAxisMesh.Material.Opacity = 1f - Easing.EaseIn(FezMath.Saturate(num4 * 2f), EasingType.Quadratic);
			if (Time != 0f)
			{
				FatAxisMesh.Scale *= MathHelper.Lerp(1.015f, 1.01f, Easing.EaseIn(num4, EasingType.Quadratic));
			}
			HelixMesh.Scale = FatAxisMesh.Scale;
			CameraManager.Center = new Vector3(0f, FatAxisMesh.Scale.X / 1000f, 0f);
			CameraManager.SnapInterpolation();
			HelixMesh.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Easing.EaseOut(0.015f * (1f - num4), EasingType.Cubic));
			float opacity = Easing.EaseIn(num4, EasingType.Cubic);
			NoiseMesh.Material.Opacity = opacity;
			NoiseMesh.Scale *= 1.0001f;
			if (num4 == 1f)
			{
				ChangeState();
			}
			break;
		}
		case State.StrandZoom:
		{
			float num3 = FezMath.Saturate(Time / 7.42f);
			if (Time != 0f && num3 != 1f)
			{
				HelixMesh.Scale *= 1.01f;
			}
			HelixMesh.Position = (HelixMesh.Scale.X - 7400f) / 1000f * CameraManager.Direction;
			CameraManager.Center = Vector3.Lerp(new Vector3(0f, HelixMesh.Scale.X / 1000f, 0f), new Vector3((0f - HelixMesh.Scale.X) / 90000f, HelixMesh.Scale.X / 1100f, 0f), num3);
			CameraManager.SnapInterpolation();
			NoiseMesh.Scale *= MathHelper.Lerp(1.0001f, 1.0025f, num3);
			ShimmeringEffect.Saturation = 1f - num3 * 0.5f;
			if (num3 == 1f)
			{
				ChangeState();
			}
			break;
		}
		}
	}

	private void ChangeState()
	{
		if (ActiveState == State.StrandZoom)
		{
			foreach (DrawableGameComponent scene in Host.Scenes)
			{
				if (scene is TetraordialOoze)
				{
					(scene as TetraordialOoze).NoiseMesh = NoiseMesh;
				}
			}
			Host.Cycle();
		}
		else
		{
			Time = 0f;
			ActiveState++;
			Update(new GameTime());
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Loading)
		{
			base.GraphicsDevice.Clear(EndCutscene32Host.PurpleBlack);
			switch (ActiveState)
			{
			case State.AxisZoom:
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.CutsceneWipe);
				HelixMesh.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.CutsceneWipe);
				base.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
				TargetRenderer.DrawFullscreen(PurpleGradientTexture);
				NoiseMesh.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
				FatAxisMesh.Draw();
				break;
			case State.StrandZoom:
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.CutsceneWipe);
				HelixMesh.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.CutsceneWipe);
				base.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
				TargetRenderer.DrawFullscreen(PurpleGradientTexture, new Color(1f, 1f, 1f, 1f - FezMath.Saturate(Time / 7.42f)));
				NoiseMesh.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
				break;
			}
		}
	}
}
