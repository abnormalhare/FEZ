using System;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene32;

internal class Pixelizer : DrawableGameComponent
{
	private enum State
	{
		Wait,
		Zooming
	}

	private const float ZoomStepDuration = 3f;

	private readonly EndCutscene32Host Host;

	private RenderTarget2D LowResRT;

	private Mesh GoMesh;

	private Group GomezGroup;

	private Group FezGroup;

	private float TotalTime;

	private float StepTime;

	private State ActiveState;

	private int ZoomStep;

	private float InitialRadius;

	private float OldSfxVol;

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	public Pixelizer(Game game, EndCutscene32Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
		base.UpdateOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		GoMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false
		};
		DrawActionScheduler.Schedule(delegate
		{
			GoMesh.Effect = new DefaultEffect.VertexColored
			{
				Fullbright = true
			};
			GoMesh.Effect.ForcedViewMatrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, -249.95f, 1f);
			GoMesh.Effect.ForcedProjectionMatrix = new Matrix(0.2f, 0f, 0f, 0f, 0f, 0.3555556f, 0f, 0f, 0f, 0f, -0.0020004f, 0f, 0f, 0f, -0.00020004f, 1f);
		});
		GomezGroup = GoMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, Color.White, centeredOnOrigin: true, doublesided: false, crosshatch: false);
		FezGroup = GoMesh.AddFace(Vector3.One / 2f, Vector3.Zero, FaceOrientation.Front, Color.Red, centeredOnOrigin: true, doublesided: false, crosshatch: false);
		LevelManager.ActualAmbient = new Color(0.25f, 0.25f, 0.25f);
		LevelManager.ActualDiffuse = Color.White;
		OldSfxVol = SoundManager.SoundEffectVolume;
		Reset();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		LowResRT.Dispose();
		LowResRT = null;
		if (GoMesh != null)
		{
			GoMesh.Dispose();
		}
		GoMesh = null;
	}

	private void Reset()
	{
		CameraManager.PixelsPerTrixel = 2f;
		CameraManager.SnapInterpolation();
		InitialRadius = CameraManager.Radius;
		GomezGroup.Position = Vector3.Zero;
		FezGroup.Position = new Vector3(-0.25f, 0.75f, 0f);
		GoMesh.Scale = Vector3.One;
		Group fezGroup = FezGroup;
		Quaternion rotation = (GomezGroup.Rotation = Quaternion.Identity);
		fezGroup.Rotation = rotation;
		CameraManager.Center = Vector3.Zero;
		CameraManager.Direction = Vector3.UnitZ;
		CameraManager.Radius = 10f;
		ZoomStep = 1;
		TotalTime = 0f;
		StepTime = 0f;
		RescaleRT();
	}

	private void RescaleRT()
	{
		if (LowResRT != null)
		{
			TargetRenderer.UnscheduleHook(LowResRT);
			LowResRT.Dispose();
		}
		float viewHScale = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		float viewVScale = (float)base.GraphicsDevice.Viewport.Height / (720f * base.GraphicsDevice.GetViewScale());
		DrawActionScheduler.Schedule(delegate
		{
			LowResRT = new RenderTarget2D(base.GraphicsDevice, FezMath.Round((float)(1280 / ZoomStep) * viewHScale), FezMath.Round((float)(720 / ZoomStep) * viewVScale), mipMap: false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
			if (ActiveState == State.Zooming)
			{
				TargetRenderer.ScheduleHook(base.DrawOrder, LowResRT);
			}
		});
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		StepTime += num;
		float num2 = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		if (ActiveState == State.Wait)
		{
			CameraManager.Center = PlayerManager.Center + new Vector3(0f, 2f, 0f);
			if (StepTime > 5f)
			{
				ChangeState();
			}
		}
		else if (ActiveState == State.Zooming)
		{
			TotalTime += num;
			PlayerManager.FullBright = true;
			if ((double)StepTime > 3.0 / Math.Max(Math.Pow((float)ZoomStep / 10f, 2.0), 1.0))
			{
				RescaleRT();
				ZoomStep++;
				StepTime = 0f;
			}
			CameraManager.Radius = MathHelper.Lerp(InitialRadius, 6f * base.GraphicsDevice.GetViewScale() * num2, Easing.EaseIn(Easing.EaseOut(FezMath.Saturate(TotalTime / 57f), EasingType.Sine), EasingType.Quadratic));
			CameraManager.Center = Vector3.Lerp(PlayerManager.Center + new Vector3(0f, 2f, 0f), PlayerManager.Center, Easing.EaseOut(FezMath.Saturate(TotalTime / 57f), EasingType.Sine));
			if (TotalTime > 57f)
			{
				ChangeState();
			}
		}
	}

	private void ChangeState()
	{
		if (ActiveState == State.Wait)
		{
			TargetRenderer.ScheduleHook(base.DrawOrder, LowResRT);
		}
		if (ActiveState == State.Zooming)
		{
			TargetRenderer.UnscheduleHook(LowResRT);
			SoundManager.KillSounds();
			SoundManager.SoundEffectVolume = OldSfxVol;
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
		if (ActiveState == State.Wait || GameState.Loading)
		{
			return;
		}
		if (GameState.Paused)
		{
			if (TargetRenderer.IsHooked(LowResRT))
			{
				TargetRenderer.Resolve(LowResRT, reschedule: true);
				base.GraphicsDevice.Clear(Color.Black);
				base.GraphicsDevice.SetupViewport();
				base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
				TargetRenderer.DrawFullscreen(LowResRT);
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			}
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.Gomez);
		Vector3 vector = EndCutscene32Host.PurpleBlack.ToVector3();
		graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		TargetRenderer.DrawFullscreen(new Color(vector.X, vector.Y, vector.Z, Easing.EaseIn(Easing.EaseOut(FezMath.Saturate(TotalTime / 57f), EasingType.Sine), EasingType.Quartic)));
		graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		SoundManager.SoundEffectVolume = 1f - Easing.EaseIn(FezMath.Saturate(TotalTime / 57f), EasingType.Quadratic) * 0.9f;
		if (TotalTime > 54f && TotalTime < 57f)
		{
			PlayerManager.Hidden = true;
			GoMesh.Draw();
		}
		if (TargetRenderer.IsHooked(LowResRT))
		{
			TargetRenderer.Resolve(LowResRT, reschedule: true);
			base.GraphicsDevice.Clear(Color.Black);
			base.GraphicsDevice.SetupViewport();
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
			graphicsDevice.SetBlendingMode(BlendingMode.Opaque);
			TargetRenderer.DrawFullscreen(LowResRT);
			graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		}
		if (TotalTime > 57f)
		{
			PlayerManager.Hidden = true;
			GoMesh.Draw();
		}
	}
}
