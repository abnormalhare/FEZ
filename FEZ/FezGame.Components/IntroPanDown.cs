using System;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class IntroPanDown : GameComponent
{
	public float SinceStarted;

	private Vector3 Origin;

	private Vector3 Destination;

	private float Distance;

	public bool DoPanDown;

	public bool IsDisposed;

	private bool FirstConstraint;

	private SoundEmitter ePanDown;

	private Vector3 lastPosition;

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IDotManager Dot { get; set; }

	public IntroPanDown(Game game)
		: base(game)
	{
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (ePanDown != null && !ePanDown.Dead)
		{
			ePanDown.FadeOutAndDie(0.1f, autoPause: false);
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		GameState.InCutscene = false;
		GameState.ForceTimePaused = true;
		PlayerManager.CanControl = false;
		Destination = CameraManager.Center;
		RecalculateOrigin();
		CameraManager.Center = Origin;
		if (CameraManager.Constrained)
		{
			FirstConstraint = true;
		}
		SoundManager.PlayNewAmbience();
		ePanDown = CMProvider.Get(CM.Intro).Load<SoundEffect>("Sounds/Intro/PanDown").Emit(loop: true, paused: true);
	}

	private void RecalculateOrigin()
	{
		bool flag = LevelManager.Sky != null && (LevelManager.Sky.Name == "GRAVE" || LevelManager.Sky.Name == "MINE");
		float num = CameraManager.Radius / CameraManager.AspectRatio * 0.5f;
		Origin = Destination * FezMath.XZMask;
		Origin.Y = (flag ? (0f - num) : (LevelManager.Size.Y + num));
		Distance = Math.Abs(Origin.Y - Destination.Y);
		if (Distance <= 0f)
		{
			Distance = 1f;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		if (LevelManager.WaterType != 0)
		{
			LiquidHost.Instance.ForcedUpdate = true;
			LiquidHost.Instance.Update(gameTime);
			LiquidHost.Instance.ForcedUpdate = false;
		}
		if (CameraManager.Constrained && !FirstConstraint)
		{
			FirstConstraint = true;
			Destination = CameraManager.Center;
			RecalculateOrigin();
		}
		if (!DoPanDown)
		{
			CameraManager.Center = Origin;
			return;
		}
		if (ePanDown == null || ePanDown.Dead)
		{
			ServiceHelper.RemoveComponent(this);
			return;
		}
		if (ePanDown.Cue.State != 0)
		{
			ePanDown.Cue.Resume();
		}
		if (!GameState.Paused)
		{
			SinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds * 8f;
		}
		CameraManager.Center = Vector3.Lerp(Origin, Destination, Easing.EaseInOut(SinceStarted / Distance, EasingType.Quadratic, EasingType.Sine));
		if (lastPosition != Vector3.Zero)
		{
			ePanDown.VolumeFactor = FezMath.Saturate((lastPosition.Y - CameraManager.Center.Y) * 2f);
			ePanDown.Pitch = -1f + FezMath.Saturate((lastPosition.Y - CameraManager.Center.Y) * 2f) * 2f;
		}
		lastPosition = CameraManager.Center;
		if (!(SinceStarted >= Distance))
		{
			return;
		}
		GameState.ForceTimePaused = false;
		if (Dot.Behaviour == DotHost.BehaviourType.SpiralAroundWithCamera)
		{
			PlayerManager.CanControl = true;
		}
		else if (LevelManager.Name == "ELDERS")
		{
			while (!PlayerManager.CanControl)
			{
				PlayerManager.CanControl = true;
			}
			PlayerManager.CanControl = false;
		}
		else
		{
			while (!PlayerManager.CanControl)
			{
				PlayerManager.CanControl = true;
			}
		}
		if (LevelManager.Name == "STARGATE" && !GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(0))
		{
			SoundManager.PlayNewSong("Swell");
		}
		else
		{
			SoundManager.PlayNewSong();
		}
		IsDisposed = true;
		ServiceHelper.RemoveComponent(this);
	}
}
