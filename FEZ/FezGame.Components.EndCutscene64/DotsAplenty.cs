using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene64;

internal class DotsAplenty : DrawableGameComponent
{
	private enum State
	{
		Zooming
	}

	private const int Rows = 54;

	private Mesh DotMesh;

	private Mesh CloneMesh;

	private InstancedDotEffect DotEffect;

	private float Theta;

	private Texture2D NoiseTexture;

	private SoundEffect sAppear;

	private SoundEffect sStartMove;

	private SoundEffect sProgressiveAppear;

	private SoundEffect sNoise;

	private SoundEmitter eNoise;

	private bool sMovePlayed;

	private bool sProgPlayed;

	private float EightShapeStep;

	private VignetteEffect VignetteEffect;

	private ScanlineEffect ScanlineEffect;

	private RenderTargetHandle RtHandle;

	private readonly EndCutscene64Host Host;

	private float StepTime;

	private State ActiveState;

	private Matrix NoiseOffset;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	public DotsAplenty(Game game, EndCutscene64Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
		base.UpdateOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		MakeDot();
		DrawActionScheduler.Schedule(delegate
		{
			RtHandle = TargetRenderer.TakeTarget();
			VignetteEffect = new VignetteEffect();
			ScanlineEffect = new ScanlineEffect();
			NoiseTexture = CMProvider.Get(CM.EndCutscene).Load<Texture2D>("Other Textures/noise");
		});
		sAppear = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/DotAppear");
		sStartMove = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/DotStartMove");
		sProgressiveAppear = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/DotsProgressiveAppear");
		sNoise = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/WhiteNoise");
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		TargetRenderer.ReturnTarget(RtHandle);
		RtHandle = null;
		CloneMesh.Dispose();
		DotMesh.Dispose();
		CloneMesh = (DotMesh = null);
		VignetteEffect.Dispose();
		VignetteEffect = null;
		ScanlineEffect.Dispose();
		ScanlineEffect = null;
		DotEffect = null;
	}

	private void MakeDot()
	{
		CloneMesh = new Mesh
		{
			DepthWrites = false,
			Culling = CullMode.None,
			AlwaysOnTop = true,
			SamplerState = SamplerState.PointWrap
		};
		float aspectRatio = CameraManager.AspectRatio;
		for (int i = -10; i <= 10; i++)
		{
			for (int j = -10; j <= 10; j++)
			{
				CloneMesh.AddFace(new Vector3(600f, 600f / aspectRatio, 1f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true).Position = new Vector3(j * 575, (float)(i * 575) / aspectRatio, 0f);
			}
		}
		CloneMesh.CollapseToBuffer<FezVertexPositionNormalTexture>();
		DotMesh = new Mesh
		{
			Blending = BlendingMode.Additive,
			DepthWrites = false,
			Culling = CullMode.None,
			AlwaysOnTop = true,
			Material = 
			{
				Opacity = 0.4f
			}
		};
		DrawActionScheduler.Schedule(delegate
		{
			CloneMesh.Effect = new DefaultEffect.Textured();
			DotMesh.Effect = (DotEffect = new InstancedDotEffect());
		});
		ShaderInstancedIndexedPrimitives<VertexPosition4ColorInstance, Vector4> shaderInstancedIndexedPrimitives = (ShaderInstancedIndexedPrimitives<VertexPosition4ColorInstance, Vector4>)(DotMesh.AddGroup().Geometry = new ShaderInstancedIndexedPrimitives<VertexPosition4ColorInstance, Vector4>(PrimitiveType.TriangleList, 100));
		List<Vector4> list = new List<Vector4>
		{
			new Vector4(-1f, -1f, -1f, -1f),
			new Vector4(1f, -1f, -1f, -1f),
			new Vector4(-1f, 1f, -1f, -1f),
			new Vector4(1f, 1f, -1f, -1f),
			new Vector4(-1f, -1f, 1f, -1f),
			new Vector4(1f, -1f, 1f, -1f),
			new Vector4(-1f, 1f, 1f, -1f),
			new Vector4(1f, 1f, 1f, -1f),
			new Vector4(-1f, -1f, -1f, 1f),
			new Vector4(1f, -1f, -1f, 1f),
			new Vector4(-1f, 1f, -1f, 1f),
			new Vector4(1f, 1f, -1f, 1f),
			new Vector4(-1f, -1f, 1f, 1f),
			new Vector4(1f, -1f, 1f, 1f),
			new Vector4(-1f, 1f, 1f, 1f),
			new Vector4(1f, 1f, 1f, 1f)
		};
		int[] array = new int[96]
		{
			0, 2, 3, 1, 1, 3, 7, 5, 5, 7,
			6, 4, 4, 6, 2, 0, 0, 4, 5, 1,
			2, 6, 7, 3, 8, 10, 11, 9, 9, 11,
			15, 13, 13, 15, 14, 12, 12, 14, 10, 8,
			8, 12, 13, 9, 10, 14, 15, 11, 0, 1,
			9, 8, 0, 2, 10, 8, 2, 3, 11, 10,
			3, 1, 9, 11, 4, 5, 13, 12, 6, 7,
			15, 14, 4, 6, 14, 12, 5, 7, 15, 13,
			4, 0, 8, 12, 6, 2, 10, 14, 3, 7,
			15, 11, 1, 5, 13, 9
		};
		shaderInstancedIndexedPrimitives.Vertices = new VertexPosition4ColorInstance[96];
		for (int k = 0; k < 4; k++)
		{
			for (int l = 0; l < 6; l++)
			{
				Vector3 vector = Vector3.Zero;
				switch ((l + k * 6) % 6)
				{
				case 0:
					vector = new Vector3(0f, 1f, 0.75f);
					break;
				case 1:
					vector = new Vector3(1f / 6f, 1f, 0.75f);
					break;
				case 2:
					vector = new Vector3(1f / 3f, 1f, 0.75f);
					break;
				case 3:
					vector = new Vector3(0.5f, 1f, 0.75f);
					break;
				case 4:
					vector = new Vector3(2f / 3f, 1f, 0.75f);
					break;
				case 5:
					vector = new Vector3(5f / 6f, 1f, 0.75f);
					break;
				}
				for (int m = 0; m < 4; m++)
				{
					int num = m + l * 4 + k * 24;
					shaderInstancedIndexedPrimitives.Vertices[num].Color = new Color(vector.X, vector.Y, vector.Z);
					shaderInstancedIndexedPrimitives.Vertices[num].Position = list[array[num]];
				}
			}
		}
		shaderInstancedIndexedPrimitives.Indices = new int[144]
		{
			0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
			7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
			13, 12, 15, 14, 16, 17, 18, 16, 18, 19,
			20, 22, 21, 20, 23, 22, 24, 26, 25, 24,
			27, 26, 28, 30, 29, 28, 31, 30, 32, 34,
			33, 32, 35, 34, 36, 38, 37, 36, 39, 38,
			40, 41, 42, 40, 42, 43, 44, 46, 45, 44,
			47, 46, 48, 50, 49, 48, 51, 50, 52, 54,
			53, 52, 55, 54, 56, 58, 57, 56, 59, 58,
			60, 62, 61, 60, 63, 62, 64, 65, 66, 64,
			66, 67, 68, 70, 69, 68, 71, 70, 72, 74,
			73, 72, 75, 74, 76, 78, 77, 76, 79, 78,
			80, 82, 81, 80, 83, 82, 84, 86, 85, 84,
			87, 86, 88, 89, 90, 88, 90, 91, 92, 94,
			93, 92, 95, 94
		};
		int num2 = (int)Math.Floor(aspectRatio * 54f);
		int num3 = 54 * num2;
		shaderInstancedIndexedPrimitives.Instances = new Vector4[num3];
		int num4 = (int)Math.Floor(27.0);
		int num5 = (int)Math.Floor((double)num2 / 2.0);
		Random random = RandomHelper.Random;
		for (int n = -num4; n < num4; n++)
		{
			for (int num6 = -num5; num6 < num5; num6++)
			{
				int num7 = (n + num4) * num2 + (num6 + num5);
				shaderInstancedIndexedPrimitives.Instances[num7] = new Vector4(num6 * 6, n * 6 + ((Math.Abs(num6) % 2 != 0) ? 3 : 0), 0f, (num6 == 0 && n == 0) ? 0f : ((float)random.NextDouble() * (float)Math.PI * 2f));
			}
		}
		shaderInstancedIndexedPrimitives.InstanceCount = num3;
		shaderInstancedIndexedPrimitives.InstancesDirty = true;
		shaderInstancedIndexedPrimitives.UpdateBuffers();
	}

	private void Reset()
	{
		CameraManager.Center = Vector3.Zero;
		CameraManager.Direction = Vector3.UnitZ;
		CameraManager.Radius = 0.0625f;
		CameraManager.SnapInterpolation();
		DotMesh.Position = Vector3.Zero;
		DotMesh.Scale = Vector3.One;
		sMovePlayed = (sProgPlayed = false);
		if (eNoise != null && !eNoise.Dead)
		{
			eNoise.FadeOutAndDie(0f);
		}
		eNoise = null;
		StepTime = 0f;
		Theta = 0f;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		if (num == 0f || StepTime == 0f)
		{
			Reset();
			sAppear.Emit();
		}
		StepTime += num;
		if (CameraManager.Radius < 11000f)
		{
			GameState.SkyRender = true;
			CameraManager.Radius *= 1.00625f;
			CameraManager.SnapInterpolation();
			GameState.SkyRender = false;
		}
		if (!sMovePlayed && StepTime >= 4f)
		{
			sStartMove.Emit();
			sMovePlayed = true;
		}
		if (!sProgPlayed && StepTime >= 14f)
		{
			sProgressiveAppear.Emit();
			sProgPlayed = true;
		}
		if (StepTime >= 29f)
		{
			if (eNoise == null)
			{
				eNoise = sNoise.Emit(loop: true, 0f, 0f);
			}
			eNoise.VolumeFactor = FezMath.Saturate((StepTime - 29f) / 7f);
		}
		if (StepTime < 33f)
		{
			UpdateDot(num);
			float viewScale = base.GraphicsDevice.GetViewScale();
			if (CameraManager.Radius >= 500f)
			{
				GameState.SkyRender = true;
				float radius = CameraManager.Radius;
				CameraManager.Radius = 600f * viewScale;
				CameraManager.SnapInterpolation();
				base.GraphicsDevice.SetRenderTarget(RtHandle.Target);
				base.GraphicsDevice.Clear(ColorEx.TransparentWhite);
				DrawDot();
				base.GraphicsDevice.SetRenderTarget(null);
				CloneMesh.Texture = RtHandle.Target;
				CameraManager.Radius = radius;
				CameraManager.SnapInterpolation();
				GameState.SkyRender = false;
			}
		}
		if (StepTime > 36f)
		{
			ChangeState();
		}
	}

	private void DrawDot()
	{
		DotMesh.Blending = BlendingMode.Alphablending;
		DotMesh.Material.Diffuse = Vector3.Zero;
		DotMesh.Material.Opacity = 0.4f;
		DotMesh.Draw();
		DotMesh.Blending = BlendingMode.Additive;
		DotMesh.Material.Diffuse = Vector3.One;
		DotMesh.Material.Opacity = 1f;
		DotMesh.Draw();
	}

	private void UpdateDot(float elapsed)
	{
		float num = (float)Math.Sqrt(Math.Max((CameraManager.Radius - 50f) / 200f, 1f));
		elapsed *= num;
		DotEffect.DistanceFactor = num;
		float num2 = Easing.EaseIn(FezMath.Saturate((StepTime - 2f) / 7f), EasingType.Sine);
		Theta += elapsed * num2;
		DotEffect.Theta = Theta;
		float num3 = (float)Math.Sin(StepTime / 3f) * 0.5f + 1f;
		EightShapeStep += elapsed * num3;
		DotEffect.EightShapeStep = EightShapeStep;
		DotEffect.ImmobilityFactor = Easing.EaseIn(FezMath.Saturate((StepTime - 5f) / 10f), EasingType.Sine);
	}

	private void ChangeState()
	{
		if (ActiveState == State.Zooming)
		{
			Host.eNoise = eNoise;
			Host.Cycle();
		}
		else
		{
			StepTime = 0f;
			ActiveState++;
			Update(new GameTime());
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		base.GraphicsDevice.Clear(Color.White);
		if (StepTime < 33f)
		{
			if (CameraManager.Radius < 500f)
			{
				DrawDot();
			}
			else
			{
				CloneMesh.Draw();
			}
		}
		Vector3 vector = EndCutscene64Host.PurpleBlack.ToVector3();
		if (StepTime < 2f)
		{
			TargetRenderer.DrawFullscreen(new Color(vector.X, vector.Y, vector.Z, 1f - Easing.EaseInOut(FezMath.Saturate(StepTime / 2f), EasingType.Sine)));
		}
		if (StepTime > 27f)
		{
			int width = base.GraphicsDevice.Viewport.Width;
			int height = base.GraphicsDevice.Viewport.Height;
			NoiseOffset = new Matrix
			{
				M11 = (float)width / 1024f,
				M22 = (float)height / 512f,
				M33 = 1f,
				M44 = 1f,
				M31 = RandomHelper.Unit(),
				M32 = RandomHelper.Unit()
			};
			float num = Easing.EaseIn(FezMath.Saturate((StepTime - 27f) / 6f), EasingType.Sine);
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
			TargetRenderer.DrawFullscreen(ScanlineEffect, NoiseTexture, NoiseOffset, new Color(1f, 1f, 1f, num));
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Multiply);
			TargetRenderer.DrawFullscreen(VignetteEffect, new Color(1f, 1f, 1f, num * 0.425f));
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		}
	}
}
