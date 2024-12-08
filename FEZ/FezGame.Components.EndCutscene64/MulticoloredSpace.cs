using System;
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

internal class MulticoloredSpace : DrawableGameComponent
{
	private enum State
	{
		Zooming
	}

	private static readonly Color MainCubeColor = new Color(23f / 85f, 83f / 85f, 1f);

	private static readonly Color[] Colors = new Color[10]
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
		new Color(98, 21, 88)
	};

	private readonly EndCutscene64Host Host;

	private Mesh PointsMesh;

	private Mesh CubesMesh;

	private DefaultEffect CubesEffect;

	private SoundEffect sBlueZoomOut;

	private SoundEffect sProgressiveAppear;

	private SoundEffect sFadeOut;

	private bool sBluePlayed;

	private bool sProgPlayed;

	private bool sFadePlayed;

	private float preWaitTime;

	private float StepTime;

	private State ActiveState;

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

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public MulticoloredSpace(Game game, EndCutscene64Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
		base.UpdateOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		sBlueZoomOut = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/BlueZoomOut");
		sProgressiveAppear = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/CubesProgressiveAppear");
		sFadeOut = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/CubesFadeOut");
		LevelManager.ActualAmbient = new Color(0.25f, 0.25f, 0.25f);
		LevelManager.ActualDiffuse = Color.White;
		Random random = RandomHelper.Random;
		float num = 1f / (float)Math.Sqrt(6.0);
		float z = (float)Math.Sqrt(3.0) / 2f;
		float num2 = (float)Math.Sqrt(2.0 / 3.0);
		float num3 = 1f / (2f * (float)Math.Sqrt(3.0));
		float num4 = 1f / (float)Math.Sqrt(2.0);
		VertexPositionNormalColor[] array = new VertexPositionNormalColor[1350000];
		int[] array2 = new int[2025000];
		int num5 = 0;
		int num6 = 0;
		for (int i = -125; i < 125; i++)
		{
			for (int j = -225; j < 225; j++)
			{
				Color color = ((j != 0 || i != 0) ? Colors[random.Next(0, Colors.Length)] : MainCubeColor);
				Vector3 vector = new Vector3(j * 6, i * 6 + ((Math.Abs(j) % 2 != 0) ? 3 : 0), 0f);
				int num7 = num5;
				array[num5++] = new VertexPositionNormalColor(new Vector3(num4, 0f - num, 0f - num3) + vector, new Vector3(-1f, 0f, 0f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(num4, num, num3) + vector, new Vector3(-1f, 0f, 0f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f, 0f, z) + vector, new Vector3(-1f, 0f, 0f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f, 0f - num2, num3) + vector, new Vector3(-1f, 0f, 0f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f, 0f - num2, num3) + vector, new Vector3(0f, 0f, -1f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f, 0f, z) + vector, new Vector3(0f, 0f, -1f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f - num4, num, num3) + vector, new Vector3(0f, 0f, -1f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f - num4, 0f - num, 0f - num3) + vector, new Vector3(0f, 0f, -1f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f, num2, 0f - num3) + vector, new Vector3(0f, 1f, 0f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f - num4, num, num3) + vector, new Vector3(0f, 1f, 0f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(0f, 0f, z) + vector, new Vector3(0f, 1f, 0f), color);
				array[num5++] = new VertexPositionNormalColor(new Vector3(num4, num, num3) + vector, new Vector3(0f, 1f, 0f), color);
				array2[num6++] = num7;
				array2[num6++] = 2 + num7;
				array2[num6++] = 1 + num7;
				array2[num6++] = num7;
				array2[num6++] = 3 + num7;
				array2[num6++] = 2 + num7;
				array2[num6++] = 4 + num7;
				array2[num6++] = 6 + num7;
				array2[num6++] = 5 + num7;
				array2[num6++] = 4 + num7;
				array2[num6++] = 7 + num7;
				array2[num6++] = 6 + num7;
				array2[num6++] = 8 + num7;
				array2[num6++] = 10 + num7;
				array2[num6++] = 9 + num7;
				array2[num6++] = 8 + num7;
				array2[num6++] = 11 + num7;
				array2[num6++] = 10 + num7;
			}
		}
		CubesMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true
		};
		Group group = CubesMesh.AddGroup();
		BufferedIndexedPrimitives<VertexPositionNormalColor> bufferedIndexedPrimitives = new BufferedIndexedPrimitives<VertexPositionNormalColor>(array, array2, PrimitiveType.TriangleList);
		bufferedIndexedPrimitives.UpdateBuffers();
		bufferedIndexedPrimitives.CleanUp();
		group.Geometry = bufferedIndexedPrimitives;
		PointsMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true
		};
		Color[] array3 = new Color[32640];
		Vector3[] array4 = new Vector3[32640];
		int num8 = 0;
		for (int k = -68; k < 68; k++)
		{
			for (int l = -120; l < 120; l++)
			{
				array4[num8] = new Vector3((float)l / 8f, (float)k / 8f + ((Math.Abs(l) % 2 == 0) ? 0f : 0.0625f), 0f);
				array3[num8++] = RandomHelper.InList(Colors);
			}
		}
		PointsMesh.AddPoints(array3, array4, buffered: true);
		DrawActionScheduler.Schedule(delegate
		{
			CubesMesh.Effect = (CubesEffect = new DefaultEffect.LitVertexColored());
			PointsMesh.Effect = new PointsFromLinesEffect();
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		CubesMesh.Dispose();
		PointsMesh.Dispose();
	}

	private void Reset()
	{
		CameraManager.Center = Vector3.Zero;
		CameraManager.Direction = Vector3.UnitZ;
		CameraManager.Radius = 1.25f;
		CameraManager.SnapInterpolation();
		PointsMesh.Scale = Vector3.One;
		sBluePlayed = (sProgPlayed = (sFadePlayed = false));
		preWaitTime = 0f;
		StepTime = 0f;
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Loading && !GameState.Paused)
		{
			float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (num == 0f || StepTime == 0f)
			{
				Reset();
			}
			GameState.SkipRendering = true;
			if (preWaitTime > 2f)
			{
				StepTime += num;
				CameraManager.Radius *= 1.005f;
				PointsMesh.Scale *= 1.00125f;
			}
			else
			{
				StepTime = 0.001f;
				preWaitTime += num;
			}
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			CubesEffect.Emissive = 1f - Easing.EaseInOut(FezMath.Saturate(StepTime / 10f), EasingType.Quadratic);
			CubesMesh.Material.Opacity = 1f - Easing.EaseIn(FezMath.Saturate((StepTime - 23f) / 3f), EasingType.Sine);
			PointsMesh.Material.Opacity = 1f - Easing.EaseIn(FezMath.Saturate((StepTime - 5f) / 10f), EasingType.Sine);
			if (!sBluePlayed && StepTime > 0.25f)
			{
				sBlueZoomOut.Emit();
				sBluePlayed = true;
			}
			if (!sProgPlayed && StepTime > 7.5f)
			{
				sProgressiveAppear.Emit();
				sProgPlayed = true;
			}
			if (!sFadePlayed && StepTime > 24f)
			{
				sFadeOut.Emit();
				sFadePlayed = true;
			}
			if (StepTime > 26f)
			{
				ChangeState();
			}
		}
	}

	private void ChangeState()
	{
		if (ActiveState == State.Zooming)
		{
			Host.Cycle();
			return;
		}
		StepTime = 0f;
		ActiveState++;
		Update(new GameTime());
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Loading)
		{
			base.GraphicsDevice.Clear(EndCutscene64Host.PurpleBlack);
			if (PointsMesh.Material.Opacity > 0f)
			{
				PointsMesh.Draw();
			}
			CubesMesh.Draw();
		}
	}
}
