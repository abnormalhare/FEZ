using System;
using System.Linq;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene32;

internal class FezGrid : DrawableGameComponent
{
	private enum State
	{
		Wait,
		RotateZoom,
		GridFadeAlign,
		CubeRise
	}

	private const float SeparateDuration = 2f;

	private const float RotateZoomDuration = 12f;

	private const float GridFadeAlignDuration = 12f;

	private const float CubeRiseDuration = 12f;

	private readonly EndCutscene32Host Host;

	private Mesh GoMesh;

	private Group GomezGroup;

	private Group FezGroup;

	private Mesh TetraMesh;

	private Mesh CubeMesh;

	private Mesh StencilMesh;

	private float Time;

	private State ActiveState;

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public FezGrid(Game game, EndCutscene32Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		GoMesh = new Mesh();
		GomezGroup = GoMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, Color.White, centeredOnOrigin: true, doublesided: false, crosshatch: false);
		FezGroup = GoMesh.AddFace(Vector3.One / 2f, Vector3.Zero, FaceOrientation.Front, Color.Red, centeredOnOrigin: true, doublesided: false, crosshatch: false);
		TetraMesh = new Mesh();
		Vector3[] array = new Vector3[2560];
		for (int i = 0; i < 1280; i++)
		{
			array[i * 2] = new Vector3(-640f, 0f, i - 640);
			array[i * 2 + 1] = new Vector3(640f, 0f, i - 640);
		}
		Color[] pointColors = Enumerable.Repeat(Color.Red, 2560).ToArray();
		TetraMesh.AddLines(pointColors, array, buffered: true);
		TetraMesh.AddLines(pointColors, array.Select((Vector3 v) => new Vector3(v.Z, 0f, v.X)).ToArray(), buffered: true);
		CubeMesh = new Mesh();
		CubeMesh.AddFlatShadedBox(Vector3.One, Vector3.Zero, Color.White, centeredOnOrigin: true);
		StencilMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false
		};
		StencilMesh.AddFace(FezMath.XZMask * 1280f, Vector3.Zero, FaceOrientation.Top, centeredOnOrigin: true);
		DrawActionScheduler.Schedule(delegate
		{
			GoMesh.Effect = new DefaultEffect.VertexColored
			{
				Fullbright = true
			};
			TetraMesh.Effect = new DefaultEffect.VertexColored
			{
				Fullbright = true
			};
			CubeMesh.Effect = new DefaultEffect.LitVertexColored();
			StencilMesh.Effect = new DefaultEffect.Textured();
		});
		LevelManager.ActualAmbient = new Color(0.25f, 0.25f, 0.25f);
		LevelManager.ActualDiffuse = Color.White;
		Reset();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (TetraMesh != null)
		{
			TetraMesh.Dispose();
		}
		if (GoMesh != null)
		{
			GoMesh.Dispose();
		}
		if (CubeMesh != null)
		{
			CubeMesh.Dispose();
		}
		if (StencilMesh != null)
		{
			StencilMesh.Dispose();
		}
		TetraMesh = (GoMesh = (CubeMesh = (StencilMesh = null)));
	}

	private void Reset()
	{
		GomezGroup.Position = Vector3.Zero;
		FezGroup.Position = new Vector3(-0.25f, 0.75f, 0f);
		GoMesh.Scale = Vector3.One;
		Group fezGroup = FezGroup;
		Quaternion rotation = (GomezGroup.Rotation = Quaternion.Identity);
		fezGroup.Rotation = rotation;
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		CameraManager.Center = Vector3.Zero;
		CameraManager.Direction = Vector3.UnitZ;
		CameraManager.Radius = 10f * base.GraphicsDevice.GetViewScale() * num;
		CameraManager.SnapInterpolation();
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
		case State.Wait:
			Reset();
			if (Time > 2f)
			{
				ChangeState();
			}
			break;
		case State.RotateZoom:
		{
			float num3 = FezMath.Saturate(Time / 12f);
			float amount2 = num3;
			GomezGroup.Rotation = Quaternion.Slerp(Quaternion.Identity, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.PI / 4f), amount2);
			FezGroup.Rotation = Quaternion.Slerp(Quaternion.Identity, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.PI / 2f), amount2);
			amount2 = Easing.EaseOut(num3, EasingType.Cubic);
			GomezGroup.Position = Vector3.Lerp(Vector3.Zero, new Vector3(0.5f, -1f, 0f) / 2f, amount2);
			FezGroup.Position = Vector3.Lerp(new Vector3(-0.25f, 0.75f, 0f), new Vector3(-1.5f, 1.5f, 0f) / 2f, amount2);
			GoMesh.Scale = Vector3.Lerp(Vector3.One, new Vector3(40f), Easing.EaseIn(num3, EasingType.Quartic));
			amount2 = Easing.EaseInOut(FezMath.Saturate(num3 * 2f), EasingType.Sine);
			CameraManager.Center = Vector3.Lerp(Vector3.Zero, Vector3.Transform(FezGroup.Position, GoMesh.WorldMatrix), amount2);
			CameraManager.SnapInterpolation();
			if (Time > 12f)
			{
				ChangeState();
			}
			break;
		}
		case State.GridFadeAlign:
		{
			float num4 = FezMath.Saturate(Time / 12f);
			float amount3 = FezMath.Saturate(FezMath.Saturate(num4 - 0.3f) / 0.6f);
			TetraMesh.Effect.ForcedProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Lerp((float)Math.PI / 2f, (float)Math.PI * 3f / 4f, amount3), CameraManager.AspectRatio, 0.1f, 2000f);
			float amount4 = Easing.EaseOut(num4, EasingType.Quintic);
			float amount5 = Easing.EaseInOut(FezMath.Saturate(FezMath.Saturate(num4 - 0.5f) / 0.5f), EasingType.Sine, EasingType.Sine);
			float step = Easing.EaseInOut(FezMath.Saturate(FezMath.Saturate(num4 - 0.4f) / 0.6f), EasingType.Sine, EasingType.Sine);
			Vector3 vector2 = Vector3.Lerp(Vector3.UnitY * 360f, Vector3.UnitY * 2f, amount4) + Vector3.UnitX * num4 * 100f;
			TetraMesh.Effect.ForcedViewMatrix = Matrix.CreateLookAt(vector2, Vector3.Lerp(vector2 - Vector3.UnitY, vector2 + Vector3.UnitX - Vector3.UnitY, amount5), FezMath.Slerp(new Vector3((float)Math.Sin(num4 * (float)Math.PI * 0.7f), 0f, (float)Math.Cos(num4 * (float)Math.PI * 0.7f)), Vector3.UnitY, step));
			if (Time > 12f)
			{
				ChangeState();
			}
			break;
		}
		case State.CubeRise:
		{
			float num2 = FezMath.Saturate(Time / 12f);
			CubeMesh.Position = CameraManager.Center;
			CubeMesh.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.One, Vector3.Zero, Vector3.Up));
			CubeMesh.Scale = Vector3.Lerp(Vector3.One, Vector3.One * 2f, Easing.EaseIn(FezMath.Saturate(num2 - 0.6f) / 0.4f, EasingType.Quadratic));
			TetraMesh.Effect.ForcedProjectionMatrix = Matrix.CreatePerspectiveFieldOfView((float)Math.PI * 3f / 4f, CameraManager.AspectRatio, 0.1f, 2000f);
			float amount = Easing.EaseIn(num2, EasingType.Quadratic);
			Vector3 vector = Vector3.UnitY * 2f + Vector3.UnitX * num2 * 100f;
			TetraMesh.Effect.ForcedViewMatrix = Matrix.CreateLookAt(vector, vector + Vector3.UnitX + Vector3.UnitY * MathHelper.Lerp(-1f, 3f, amount), Vector3.UnitY);
			StencilMesh.Effect.ForcedProjectionMatrix = TetraMesh.Effect.ForcedProjectionMatrix;
			StencilMesh.Effect.ForcedViewMatrix = TetraMesh.Effect.ForcedViewMatrix;
			if (Time > 12f)
			{
				ChangeState();
			}
			break;
		}
		}
	}

	private void ChangeState()
	{
		if (ActiveState == State.CubeRise)
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
		if (!GameState.Loading)
		{
			base.GraphicsDevice.Clear(EndCutscene32Host.PurpleBlack);
			switch (ActiveState)
			{
			case State.Wait:
			case State.RotateZoom:
				GoMesh.Draw();
				break;
			case State.GridFadeAlign:
				TetraMesh.Draw();
				break;
			case State.CubeRise:
				TetraMesh.Draw();
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.CutsceneWipe);
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
				StencilMesh.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.CutsceneWipe);
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
				CubeMesh.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
				break;
			}
		}
	}
}
