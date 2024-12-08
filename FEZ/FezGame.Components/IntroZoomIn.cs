using System;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class IntroZoomIn : DrawableGameComponent
{
	private TimeSpan SinceStarted;

	private float Scale;

	private float Opacity;

	private SoundEffect sZoomToHouse;

	private ZoomInEffect zoomInEffect;

	private RenderTargetHandle zoomRtHandle;

	public bool IsDisposed { get; private set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public IntroZoomIn(Game game)
		: base(game)
	{
		base.DrawOrder = 2005;
	}

	public override void Initialize()
	{
		base.Initialize();
		IGameStateManager gameState = GameState;
		bool inCutscene = (GameState.ForceTimePaused = false);
		gameState.InCutscene = inCutscene;
		PlayerManager.Action = ActionType.SleepWake;
		PlayerManager.LookingDirection = HorizontalDirection.Left;
		PlayerManager.Position += CameraManager.Viewpoint.RightVector() * -3f / 16f;
		sZoomToHouse = CMProvider.Get(CM.Intro).Load<SoundEffect>("Sounds/Intro/ZoomToHouse");
		sZoomToHouse.Emit();
		zoomInEffect = new ZoomInEffect();
		zoomRtHandle = TargetRenderingManager.TakeTarget();
		TargetRenderingManager.ScheduleHook(base.DrawOrder, zoomRtHandle.Target);
	}

	protected override void Dispose(bool disposing)
	{
		IsDisposed = true;
		if (!string.IsNullOrEmpty(LevelManager.Name))
		{
			while (!PlayerManager.CanControl)
			{
				PlayerManager.CanControl = true;
			}
			SoundManager.PlayNewSong();
			SoundManager.PlayNewAmbience();
			SoundManager.MusicVolumeFactor = 1f;
		}
		TargetRenderingManager.ReturnTarget(zoomRtHandle);
		TargetRenderingManager.UnscheduleHook(zoomRtHandle.Target);
		zoomRtHandle = null;
		zoomInEffect.Dispose();
		zoomInEffect = null;
		base.Dispose(disposing);
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused)
		{
			return;
		}
		SinceStarted += gameTime.ElapsedGameTime;
		float num = (float)SinceStarted.TotalSeconds / 6f;
		Scale = MathHelper.Lerp(200f, 1f, Easing.EaseOut(FezMath.Saturate(num), EasingType.Cubic));
		Opacity = FezMath.Saturate(num / 0.5f);
		if (num >= 1f)
		{
			if (LevelManager.Name == "GOMEZ_HOUSE_2D" && GameState.SaveData.IsNewGamePlus)
			{
				GameState.OnHudElementChanged();
			}
			ServiceHelper.RemoveComponent(this);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		TargetRenderingManager.Resolve(zoomRtHandle.Target, reschedule: true);
		TargetRenderingManager.DrawFullscreen(Color.Black);
		Matrix matrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -0.5f, -0.5f, 1f, 0f, 0f, 0f, 0f, 1f);
		Matrix matrix2 = new Matrix(Scale, 0f, 0f, 0f, 0f, Scale, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
		Matrix matrix3 = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0.5f, 0.5f, 1f, 0f, 0f, 0f, 0f, 1f);
		base.GraphicsDevice.SamplerStates[0] = SamplerStates.PointMipWrap;
		base.GraphicsDevice.SetupViewport();
		TargetRenderingManager.DrawFullscreen(zoomInEffect, zoomRtHandle.Target, matrix * matrix2 * matrix3, new Color(1f, 1f, 1f, Opacity));
	}
}
