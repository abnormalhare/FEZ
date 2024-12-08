using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class FarawayTransition : DrawableGameComponent
{
	private class LevelFader : DrawableGameComponent
	{
		private readonly RenderTargetHandle SkyRt;

		private readonly RenderTargetHandle LevelRt;

		[ServiceDependency]
		public ITargetRenderingManager TargetRenderer { get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { get; set; }

		public LevelFader(Game game, RenderTargetHandle SkyRt, RenderTargetHandle LevelRt)
			: base(game)
		{
			base.DrawOrder = 1000;
			this.SkyRt = SkyRt;
			this.LevelRt = LevelRt;
		}

		public override void Draw(GameTime gameTime)
		{
			float alpha = FezMath.Saturate((GameState.FarawaySettings.OriginFadeOutStep < 1f) ? (1f - GameState.FarawaySettings.OriginFadeOutStep) : GameState.FarawaySettings.DestinationCrossfadeStep);
			TargetRenderer.Resolve(LevelRt.Target, reschedule: true);
			base.GraphicsDevice.SetupViewport();
			base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
			TargetRenderer.DrawFullscreen(SkyRt.Target);
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			TargetRenderer.DrawFullscreen(LevelRt.Target, new Color(1f, 1f, 1f, alpha));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		}
	}

	private const float TotalDuration = 6f;

	private const float FadeOutDuration = 0.75f;

	private const float CrossfadeInStart = 4.5f;

	private const float CrossfadeInDuration = 0.75f;

	private TimeSpan SinceStarted;

	private Volume StartVolume;

	private float OriginalRadius;

	private float DestinationRadius;

	private RenderTargetHandle SkyRt;

	private RenderTargetHandle LevelRt;

	private LevelFader Fader;

	private string NextLevel;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public IDotManager DotManager { private get; set; }

	public FarawayTransition(Game game)
		: base(game)
	{
		base.DrawOrder = 1;
		ITargetRenderingManager targetRenderingManager = ServiceHelper.Get<ITargetRenderingManager>();
		SkyRt = targetRenderingManager.TakeTarget();
		targetRenderingManager.ScheduleHook(base.DrawOrder, SkyRt.Target);
	}

	public override void Initialize()
	{
		base.Initialize();
		PlayerManager.Hidden = true;
		PlayerManager.CanControl = false;
		GameState.FarawaySettings.InTransition = true;
		StartVolume = LevelManager.Volumes[PlayerManager.DoorVolume.Value];
		if (StartVolume.ActorSettings == null)
		{
			StartVolume.ActorSettings = new VolumeActorSettings();
		}
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		OriginalRadius = CameraManager.Radius;
		GameState.FarawaySettings.DestinationRadius = (DestinationRadius = ((StartVolume.ActorSettings.DestinationRadius == 0f) ? CameraManager.Radius : (StartVolume.ActorSettings.DestinationRadius * viewScale * num)));
		GameState.FarawaySettings.DestinationPixelsPerTrixel = StartVolume.ActorSettings.DestinationPixelsPerTrixel;
		GameState.FarawaySettings.SkyRt = SkyRt.Target;
		NextLevel = PlayerManager.NextLevel;
		LevelRt = TargetRenderer.TakeTarget();
		ServiceHelper.AddComponent(Fader = new LevelFader(base.Game, SkyRt, LevelRt));
		TargetRenderer.ScheduleHook(Fader.DrawOrder, LevelRt.Target);
		if (StartVolume.ActorSettings.WaterLocked)
		{
			LiquidHost.Instance.StartTransition();
		}
		if (LevelManager.Rainy && RainHost.Instance != null)
		{
			RainHost.Instance.StartTransition();
		}
		if (StartVolume.ActorSettings.DestinationSong != LevelManager.SongName)
		{
			SoundManager.PlayNewSong(null, 10f);
		}
		foreach (SoundEmitter emitter in SoundManager.Emitters)
		{
			emitter.FadeOutAndDie(1f, autoPause: false);
		}
		CMProvider.Global.Load<SoundEffect>("Sounds/Intro/ZoomToFarawayPlace").Emit().Persistent = true;
		DotManager.ForceDrawOrder(1001);
		DotManager.Burrow();
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap)
		{
			return;
		}
		SinceStarted += gameTime.ElapsedGameTime;
		GameState.FarawaySettings.TransitionStep = FezMath.Saturate((float)SinceStarted.TotalSeconds / 6f);
		float transitionStep = GameState.FarawaySettings.TransitionStep;
		GameState.FarawaySettings.OriginFadeOutStep = FezMath.Saturate((float)SinceStarted.TotalSeconds / 0.75f);
		GameState.FarawaySettings.DestinationCrossfadeStep = FezMath.Saturate(((float)SinceStarted.TotalSeconds - 4.5f) / 0.75f);
		if (GameState.FarawaySettings.DestinationCrossfadeStep > 0f && !PlayerManager.CanControl && PlayerManager.Action != ActionType.ExitDoor)
		{
			LevelMaterializer.PrepareFullCull();
			PlayerManager.Action = ActionType.ExitDoor;
		}
		if (GameState.FarawaySettings.DestinationCrossfadeStep > 0.5f && !PlayerManager.CanControl)
		{
			PlayerManager.CanControl = true;
		}
		if (GameState.FarawaySettings.DestinationCrossfadeStep == 1f && StartVolume.ActorSettings.WaterLocked && LiquidHost.Instance.InTransition)
		{
			LiquidHost.Instance.EndTransition();
		}
		if (GameState.FarawaySettings.OriginFadeOutStep != 1f)
		{
			CameraManager.Radius = MathHelper.Lerp(OriginalRadius, DestinationRadius / 4f, transitionStep);
		}
		if (GameState.FarawaySettings.LoadingAllowed)
		{
			GameState.Loading = true;
			Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
			worker.Finished += delegate
			{
				ThreadPool.Return(worker);
			};
			worker.Start(context: false);
			GameState.FarawaySettings.LoadingAllowed = false;
		}
		if (transitionStep == 1f)
		{
			CameraManager.PixelsPerTrixel = GameState.FarawaySettings.DestinationPixelsPerTrixel;
			DotManager.RevertDrawOrder();
			base.Enabled = false;
			Waiters.Wait(0.25, delegate
			{
				ServiceHelper.RemoveComponent(this);
			});
		}
	}

	private void DoLoad(bool dummy)
	{
		LevelManager.ChangeLevel(NextLevel);
		PlayerManager.ForceOverlapsDetermination();
		TrileInstance surface = PlayerManager.AxisCollision[VerticalDirection.Up].Surface;
		if (surface != null && surface.Trile.ActorSettings.Type == ActorType.UnlockedDoor && FezMath.OrientationFromPhi(surface.Trile.ActorSettings.Face.ToPhi() + surface.Phi) == CameraManager.Viewpoint.VisibleOrientation())
		{
			GameState.SaveData.ThisLevel.FilledConditions.UnlockedDoorCount++;
			TrileEmplacement id = surface.Emplacement + Vector3.UnitY;
			TrileInstance trileInstance = LevelManager.TrileInstanceAt(ref id);
			if (trileInstance.Trile.ActorSettings.Type == ActorType.UnlockedDoor)
			{
				GameState.SaveData.ThisLevel.FilledConditions.UnlockedDoorCount++;
			}
			LevelManager.ClearTrile(surface);
			LevelManager.ClearTrile(trileInstance);
			GameState.SaveData.ThisLevel.InactiveTriles.Add(surface.Emplacement);
			surface.ActorSettings.Inactive = true;
		}
		PlayerManager.Hidden = false;
		CameraManager.Constrained = false;
		GameState.ScheduleLoadEnd = true;
	}

	public override void Draw(GameTime gameTime)
	{
		TargetRenderer.Resolve(SkyRt.Target, reschedule: true);
		base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, ColorEx.TransparentBlack, 1f, 0);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		GameState.FarawaySettings.Reset();
		TargetRenderer.ReturnTarget(SkyRt);
		TargetRenderer.ReturnTarget(LevelRt);
		SkyRt = (LevelRt = null);
		ServiceHelper.RemoveComponent(Fader);
	}
}
