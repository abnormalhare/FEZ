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

internal class VibratingMembrane : DrawableGameComponent
{
	private enum State
	{
		GridFadeAlign,
		PanDown,
		Rotate,
		CubeReveal,
		ZoomOnCube
	}

	private const float GridFadeAlignDuration = 12f;

	private const float PanDownDuration = 6f;

	private const float RotateDuration = 6f;

	private const float CubeRevealDuration = 10f;

	private const float ZoomCubeDuration = 12f;

	private readonly EndCutscene32Host Host;

	private Mesh LinesMesh;

	private Mesh VibratingMesh;

	private Mesh CubeMesh;

	private Mesh PointsMesh;

	private VibratingEffect VibratingEffect;

	private VibratingEffect StaticEffect;

	private float Time;

	private float GridsCameraDistance = 1f;

	private State ActiveState;

	private Color BackgroundColor = EndCutscene32Host.PurpleBlack;

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public VibratingMembrane(Game game, EndCutscene32Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		LinesMesh = new Mesh();
		FezVertexPositionColor[] array = new FezVertexPositionColor[5120];
		int[] array2 = new int[5120];
		for (int i = 0; i < 1280; i++)
		{
			int num = 2 * i;
			array2[num] = num;
			array[num] = new FezVertexPositionColor(new Vector3(-640f, 0f, i - 640), Color.White);
			num++;
			array2[num] = num;
			array[num] = new FezVertexPositionColor(new Vector3(640f, 0f, i - 640), Color.White);
			num += 2559;
			array2[num] = num;
			array[num] = new FezVertexPositionColor(new Vector3(i - 640, 0f, -640f), Color.White);
			num++;
			array2[num] = num;
			array[num] = new FezVertexPositionColor(new Vector3(i - 640, 0f, 640f), Color.White);
		}
		Group group = LinesMesh.AddGroup();
		BufferedIndexedPrimitives<FezVertexPositionColor> bufferedIndexedPrimitives = new BufferedIndexedPrimitives<FezVertexPositionColor>(array, array2, PrimitiveType.LineList);
		bufferedIndexedPrimitives.UpdateBuffers();
		bufferedIndexedPrimitives.CleanUp();
		group.Geometry = bufferedIndexedPrimitives;
		Random random = RandomHelper.Random;
		VibratingMesh = new Mesh();
		array = new FezVertexPositionColor[500000];
		array2 = new int[998000];
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < 500; j++)
		{
			array2[num3++] = num2;
			for (int k = 0; k < 500; k++)
			{
				if (k != 0)
				{
					array2[num3++] = num2;
				}
				array[num2++] = new FezVertexPositionColor(new Vector3(j - 250, 0f, k - 250), new Color((byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256)));
				if (k < 498)
				{
					array2[num3++] = num2;
				}
			}
			array2[num3++] = num2;
			for (int l = 0; l < 500; l++)
			{
				if (l != 0)
				{
					array2[num3++] = num2;
				}
				array[num2++] = new FezVertexPositionColor(new Vector3(l - 250, 0f, j - 250), new Color((byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256)));
				if (l < 498)
				{
					array2[num3++] = num2;
				}
			}
		}
		Group group2 = VibratingMesh.AddGroup();
		bufferedIndexedPrimitives = new BufferedIndexedPrimitives<FezVertexPositionColor>(array, array2, PrimitiveType.LineList);
		bufferedIndexedPrimitives.UpdateBuffers();
		bufferedIndexedPrimitives.CleanUp();
		group2.Geometry = bufferedIndexedPrimitives;
		CubeMesh = new Mesh();
		CubeMesh.AddColoredBox(Vector3.One, Vector3.One, Color.White, centeredOnOrigin: true);
		array = new FezVertexPositionColor[200000];
		array2 = new int[200000];
		for (int m = 0; m < 100000; m++)
		{
			array[m * 2] = new FezVertexPositionColor(new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, 0f), ColorEx.TransparentWhite);
			array[m * 2 + 1] = new FezVertexPositionColor(array[m * 2].Position, Color.White);
			array2[m * 2] = m * 2;
			array2[m * 2 + 1] = m * 2 + 1;
		}
		PointsMesh = new Mesh
		{
			AlwaysOnTop = true
		};
		Group group3 = PointsMesh.AddGroup();
		bufferedIndexedPrimitives = new BufferedIndexedPrimitives<FezVertexPositionColor>(array, array2, PrimitiveType.LineList);
		bufferedIndexedPrimitives.UpdateBuffers();
		bufferedIndexedPrimitives.CleanUp();
		group3.Geometry = bufferedIndexedPrimitives;
		LevelManager.ActualAmbient = new Color(0.25f, 0.25f, 0.25f);
		LevelManager.ActualDiffuse = Color.White;
		DrawActionScheduler.Schedule(delegate
		{
			LinesMesh.Effect = (StaticEffect = new VibratingEffect());
			VibratingMesh.Effect = (VibratingEffect = new VibratingEffect());
			CubeMesh.Effect = new DefaultEffect.VertexColored();
			PointsMesh.Effect = new PointsFromLinesEffect();
		});
		Reset();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (LinesMesh != null)
		{
			LinesMesh.Dispose();
		}
		if (VibratingMesh != null)
		{
			VibratingMesh.Dispose();
		}
		if (PointsMesh != null)
		{
			PointsMesh.Dispose();
		}
		if (CubeMesh != null)
		{
			CubeMesh.Dispose();
		}
		LinesMesh = (VibratingMesh = (PointsMesh = (CubeMesh = null)));
	}

	private void Reset()
	{
		CameraManager.Center = Vector3.Zero;
		CameraManager.Direction = Vector3.UnitZ;
		CameraManager.Radius = 10f;
		CameraManager.SnapInterpolation();
		DrawActionScheduler.Schedule(delegate
		{
			StaticEffect.FogDensity = 0.0025f;
		});
		LinesMesh.Material.Opacity = 0f;
		BackgroundColor = EndCutscene32Host.PurpleBlack;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		Time += num;
		switch (ActiveState)
		{
		case State.GridFadeAlign:
		{
			if (Time == 0f)
			{
				Reset();
			}
			float num2 = FezMath.Saturate(Time / 12f);
			float amount = FezMath.Saturate(FezMath.Saturate(num2 - 0.3f) / 0.6f);
			Mesh obj = ((num2 < 0.25f) ? LinesMesh : VibratingMesh);
			obj.Effect.ForcedProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Lerp((float)Math.PI / 2f, (float)Math.PI * 3f / 4f, amount), CameraManager.AspectRatio, 0.1f, 2000f);
			float amount2 = Easing.EaseOut(num2, EasingType.Quintic);
			float amount3 = Easing.EaseInOut(FezMath.Saturate(FezMath.Saturate(num2 - 0.5f) / 0.5f), EasingType.Sine, EasingType.Sine);
			float step = Easing.EaseInOut(FezMath.Saturate(FezMath.Saturate(num2 - 0.4f) / 0.6f), EasingType.Sine, EasingType.Sine);
			obj.Material.Opacity = Easing.EaseIn(FezMath.Saturate(num2 * 2.5f), EasingType.Sine);
			Vector3 vector = Vector3.Lerp(Vector3.UnitY * 360f, Vector3.UnitY * 1.5f, amount2) + Vector3.UnitX * num2 * 100f;
			obj.Effect.ForcedViewMatrix = Matrix.CreateLookAt(vector, Vector3.Lerp(vector - Vector3.UnitY, vector + Vector3.UnitX - Vector3.UnitY * 3f, amount3), FezMath.Slerp(new Vector3((float)Math.Sin(num2 * (float)Math.PI * 0.7f), 0f, (float)Math.Cos(num2 * (float)Math.PI * 0.7f)), Vector3.UnitY, step));
			VibratingEffect vibratingEffect = VibratingEffect;
			float fogDensity = (StaticEffect.FogDensity = MathHelper.Lerp(0.0025f, 0.1f, Easing.EaseIn(num2, EasingType.Cubic)));
			vibratingEffect.FogDensity = fogDensity;
			VibratingEffect.TimeStep = Time;
			VibratingEffect.Intensity = FezMath.Saturate((num2 - 0.25f) / 0.75f * 0.5f);
			if (Time > 12f)
			{
				ChangeState();
			}
			break;
		}
		case State.PanDown:
		{
			float num5 = FezMath.Saturate(Time / 6f);
			VibratingEffect.TimeStep += num;
			VibratingEffect.FogDensity = 0.1f;
			VibratingEffect.Intensity = FezMath.Saturate(num5 / 2f + 0.5f);
			VibratingMesh.Effect.ForcedProjectionMatrix = Matrix.CreatePerspectiveFieldOfView((float)Math.PI * 3f / 4f, CameraManager.AspectRatio, 0.1f, 2000f);
			Vector3 vector4 = Vector3.UnitY * 1.5f + Vector3.UnitX * num5 * 50f;
			VibratingMesh.Effect.ForcedViewMatrix = Matrix.CreateLookAt(vector4, vector4 + Vector3.UnitX - Vector3.UnitY * MathHelper.Lerp(3f, 0f, Easing.EaseInOut(num5, EasingType.Quadratic)), Vector3.Up);
			if (Time > 6f)
			{
				ChangeState();
			}
			break;
		}
		case State.Rotate:
		{
			float num6 = FezMath.Saturate(Time / 6f);
			VibratingEffect.TimeStep += num;
			VibratingEffect.Intensity = 1f;
			float step2 = Easing.EaseInOut(num6, EasingType.Quadratic);
			Vector3 vector5 = Vector3.UnitY * 1.5f + Vector3.UnitX * (num6 * 50f + 50f);
			VibratingMesh.Effect.ForcedViewMatrix = Matrix.CreateLookAt(vector5, vector5 + Vector3.UnitX, FezMath.Slerp(Vector3.Up, -Vector3.UnitZ, step2));
			if (Time > 6f)
			{
				ChangeState();
			}
			break;
		}
		case State.CubeReveal:
		{
			if (Time == 0f)
			{
				PointsMesh.Blending = BlendingMode.Alphablending;
				Mesh pointsMesh = PointsMesh;
				Vector3 scale = (CubeMesh.Scale = new Vector3(0.001f, 0.001f, 0.001f));
				pointsMesh.Scale = scale;
				CubeMesh.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.One, Vector3.Zero, Vector3.Up));
			}
			CameraManager.Center = Vector3.Zero;
			CameraManager.Direction = Vector3.UnitZ;
			CameraManager.Radius = 10f;
			CameraManager.SnapInterpolation();
			float num4 = FezMath.Saturate(Time / 6f);
			VibratingEffect.TimeStep += num;
			VibratingEffect.Intensity = MathHelper.Lerp(1f, 5f, Easing.EaseIn(num4, EasingType.Quadratic));
			VibratingEffect.FogDensity = MathHelper.Lerp(0.1f, 1f, Easing.EaseIn(num4, EasingType.Cubic));
			GridsCameraDistance = MathHelper.Lerp(1f, 10f, Easing.EaseIn(num4, EasingType.Quadratic));
			CubeMesh.Position = new Vector3(0f, 0f, 100f);
			CubeMesh.Scale *= MathHelper.Lerp(1.05f, 1.01f, Easing.EaseOut(FezMath.Saturate(Time / 3f), EasingType.Quadratic));
			if (num4 > 0.5f)
			{
				CubeMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.0025f * ((num4 - 0.5f) * 2f)) * CubeMesh.Rotation;
			}
			PointsMesh.Position = CubeMesh.Position;
			PointsMesh.Scale = CubeMesh.Scale;
			PointsMesh.Rotation = CubeMesh.Rotation;
			Vector3 vector3 = Vector3.UnitY * 1.5f * GridsCameraDistance + Vector3.UnitX * (num4 * 50f + 100f);
			VibratingMesh.Effect.ForcedViewMatrix = Matrix.CreateLookAt(vector3, vector3 + Vector3.UnitX, -Vector3.UnitZ);
			if (Time > 10f)
			{
				ChangeState();
			}
			break;
		}
		case State.ZoomOnCube:
			CubeMesh.Scale *= 1.01f;
			CubeMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.0025f) * CubeMesh.Rotation;
			PointsMesh.Position = CubeMesh.Position;
			PointsMesh.Rotation = CubeMesh.Rotation;
			PointsMesh.Scale = CubeMesh.Scale;
			BackgroundColor = Color.Lerp(EndCutscene32Host.PurpleBlack, Color.Black, Time / 12f);
			if (Time > 12f)
			{
				ChangeState();
			}
			break;
		}
	}

	private void ChangeState()
	{
		if (ActiveState == State.ZoomOnCube)
		{
			Host.Cycle();
			return;
		}
		Time = 0f;
		ActiveState++;
		Update(new GameTime());
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		base.GraphicsDevice.Clear(BackgroundColor);
		switch (ActiveState)
		{
		case State.GridFadeAlign:
			if (FezMath.Saturate(Time / 12f) < 0.25f)
			{
				LinesMesh.Draw();
			}
			else
			{
				VibratingMesh.Draw();
			}
			break;
		case State.PanDown:
		case State.Rotate:
			VibratingMesh.Rotation = Quaternion.Identity;
			VibratingMesh.Draw();
			VibratingMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI);
			VibratingMesh.Position += new Vector3(0f, 3f, 0f);
			VibratingMesh.Draw();
			VibratingMesh.Position -= new Vector3(0f, 3f, 0f);
			break;
		case State.CubeReveal:
			VibratingMesh.Rotation = Quaternion.Identity;
			VibratingMesh.Draw();
			VibratingMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI);
			VibratingMesh.Position += new Vector3(0f, 3f * GridsCameraDistance, 0f);
			VibratingMesh.Draw();
			VibratingMesh.Position -= new Vector3(0f, 3f * GridsCameraDistance, 0f);
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.CutsceneWipe);
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
			CubeMesh.Draw();
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.CutsceneWipe);
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
			PointsMesh.Draw();
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			break;
		case State.ZoomOnCube:
			if (CubeMesh.Scale.X < 9f)
			{
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.CutsceneWipe);
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
				CubeMesh.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.CutsceneWipe);
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
			}
			PointsMesh.Draw();
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			break;
		}
	}
}
