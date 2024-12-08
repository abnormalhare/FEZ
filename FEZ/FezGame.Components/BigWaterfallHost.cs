using System;
using System.Linq;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components.Scripting;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class BigWaterfallHost : GameComponent, IBigWaterfallService, IScriptingBase
{
	private BackgroundPlane WaterfallPlane;

	private BackgroundPlane SplashPlane;

	private BackgroundPlane MoriaPlane;

	private int ScriptId;

	private AnimatedTexture OpenSplash;

	private AnimatedTexture OpeningSplash;

	private AnimatedTexture OpenWaterfall;

	private AnimatedTexture OpeningWaterfall;

	private SoundEffect sWaterfallOpening;

	private SoundEmitter eWaterfallClosed;

	private SoundEmitter eWaterfallOpen;

	private float sinceAlive;

	private bool opening;

	private float Top;

	private Vector3 TerminalPosition;

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	internal IScriptingManager ScriptingManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	public BigWaterfallHost(Game game)
		: base(game)
	{
		base.Enabled = false;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		OpenSplash = (OpeningSplash = (OpenWaterfall = (OpeningWaterfall = null)));
		sWaterfallOpening = null;
		eWaterfallClosed = null;
		eWaterfallOpen = null;
		WaterfallPlane = LevelManager.BackgroundPlanes.Values.FirstOrDefault((BackgroundPlane x) => x.ActorType == ActorType.BigWaterfall);
		base.Enabled = WaterfallPlane != null;
		if (!base.Enabled)
		{
			return;
		}
		MoriaPlane = LevelManager.BackgroundPlanes.Values.FirstOrDefault((BackgroundPlane x) => x.TextureName == "MORIA_GLOW");
		if (GameState.SaveData.ThisLevel.InactiveGroups.Contains(1) || GameState.SaveData.ThisLevel.InactiveVolumes.Contains(19))
		{
			MoriaPlane.Opacity = 0f;
		}
		MoriaPlane.Position -= Vector3.UnitX * 0.001f;
		Comparison<Group> oldGo = LevelMaterializer.StaticPlanesMesh.GroupOrder;
		LevelMaterializer.StaticPlanesMesh.GroupOrder = (Group x, Group y) => (x == MoriaPlane.Group) ? 1 : ((y != MoriaPlane.Group) ? oldGo(x, y) : (-1));
		bool flag = GameState.SaveData.ThisLevel.ScriptingState == "WATERFALL_OPEN";
		if (flag)
		{
			OpenSplash = CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water_giant_splash_open");
			OpenWaterfall = CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water_giant_open");
			Waiters.Wait(() => !GameState.Loading, delegate
			{
				LevelManager.Volumes[7].Enabled = true;
			});
		}
		else
		{
			ForkLoad(dummy: false);
		}
		SplashPlane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, flag ? OpenSplash : CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water_giant_splash"))
		{
			Doublesided = true
		};
		LevelManager.AddPlane(SplashPlane);
		Top = (WaterfallPlane.Position + WaterfallPlane.Scale * WaterfallPlane.Size / 2f).Dot(Vector3.UnitY);
		TerminalPosition = WaterfallPlane.Position - WaterfallPlane.Scale * WaterfallPlane.Size / 2f * Vector3.UnitY + Vector3.Transform(Vector3.UnitZ, WaterfallPlane.Rotation) / 16f;
		sinceAlive = 0f;
		if (flag)
		{
			SwapOpened();
			eWaterfallOpen = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BigWaterfallOpen").EmitAt(WaterfallPlane.Position, loop: true, 0f, 0f);
		}
		else
		{
			eWaterfallClosed = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BigWaterfallClosed").EmitAt(WaterfallPlane.Position, loop: true, 0f, 0f);
		}
	}

	private void ForkLoad(bool dummy)
	{
		OpenSplash = CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water_giant_splash_open");
		OpenWaterfall = CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water_giant_open");
		OpeningSplash = CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water_giant_splash_opening");
		OpeningWaterfall = CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water_giant_opening");
		sWaterfallOpening = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BigWaterfallOpening");
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || !CameraManager.ActionRunning)
		{
			return;
		}
		bool flag = !GameState.FarawaySettings.InTransition && !PlayerManager.Action.IsEnteringDoor();
		sinceAlive = FezMath.Saturate(sinceAlive + (float)gameTime.ElapsedGameTime.TotalSeconds / 2f * (float)(flag ? 1 : (-1)));
		if (!opening)
		{
			if (eWaterfallClosed != null)
			{
				eWaterfallClosed.VolumeFactor = sinceAlive;
			}
			if (eWaterfallOpen != null)
			{
				eWaterfallOpen.VolumeFactor = sinceAlive;
			}
		}
		if (!GameState.SaveData.ThisLevel.InactiveGroups.Contains(1) && !GameState.SaveData.ThisLevel.InactiveVolumes.Contains(19))
		{
			MoriaPlane.Opacity = TimeManager.NightContribution;
		}
		float num = LevelManager.WaterHeight - 1f + 0.3125f;
		if (TerminalPosition.Y <= num)
		{
			float num2 = Top - num;
			if (num2 <= 0f)
			{
				if (SplashPlane.Opacity != 0f)
				{
					SplashPlane.Opacity = 0f;
					WaterfallPlane.Opacity = 0f;
				}
				return;
			}
			if (SplashPlane.Opacity != 1f)
			{
				SplashPlane.Opacity = 1f;
				WaterfallPlane.Opacity = 1f;
			}
			SplashPlane.Position = num * Vector3.UnitY + SplashPlane.Size / 2f * Vector3.UnitY + FezMath.XZMask * TerminalPosition;
			WaterfallPlane.Scale = new Vector3(WaterfallPlane.Scale.X, num2 / WaterfallPlane.Size.Y, WaterfallPlane.Scale.Z);
			WaterfallPlane.Position = new Vector3(WaterfallPlane.Position.X, num + num2 / 2f, WaterfallPlane.Position.Z);
		}
		else if (SplashPlane != null && SplashPlane.Opacity != 0f)
		{
			SplashPlane.Opacity = 0f;
		}
	}

	public void ResetEvents()
	{
	}

	public LongRunningAction Open(int id)
	{
		if (id != WaterfallPlane.Id)
		{
			return null;
		}
		GameState.SaveData.ThisLevel.ScriptingState = "WATERFALL_OPEN";
		GameState.Save();
		SoundEmitter eWaterfallOpen = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BigWaterfallOpen").EmitAt(WaterfallPlane.Position, loop: true, paused: true);
		eWaterfallOpen.VolumeFactor = 0f;
		eWaterfallOpen.Cue.Play();
		opening = true;
		Waiters.Interpolate(6.0, delegate(float s)
		{
			if (eWaterfallClosed != null && !eWaterfallClosed.Dead)
			{
				eWaterfallClosed.VolumeFactor = (1f - s) * sinceAlive;
				if (!eWaterfallOpen.Dead)
				{
					eWaterfallOpen.VolumeFactor = s * sinceAlive;
				}
			}
		}).AutoPause = true;
		sWaterfallOpening.EmitAt(WaterfallPlane.Position);
		ScriptId = ScriptingManager.EvaluatedScript.Script.Id;
		LevelManager.RemovePlane(WaterfallPlane);
		LevelManager.RemovePlane(SplashPlane);
		Vector3 position = SplashPlane.Position;
		Vector3 scale = SplashPlane.Scale;
		Quaternion rotation = SplashPlane.Rotation;
		AnimatedTexture openingSplash = OpeningSplash;
		SplashPlane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, openingSplash)
		{
			Doublesided = true
		};
		LevelManager.AddPlane(SplashPlane);
		SplashPlane.Timing.Restart();
		WaterfallPlane.Timing.Loop = false;
		SplashPlane.Position = position;
		SplashPlane.Scale = scale;
		SplashPlane.Rotation = rotation;
		position = WaterfallPlane.Position;
		scale = WaterfallPlane.Scale;
		rotation = WaterfallPlane.Rotation;
		openingSplash = OpeningWaterfall;
		WaterfallPlane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, openingSplash)
		{
			Doublesided = true
		};
		LevelManager.AddPlane(WaterfallPlane);
		WaterfallPlane.Timing.Restart();
		WaterfallPlane.Timing.Loop = false;
		WaterfallPlane.YTextureRepeat = true;
		WaterfallPlane.Position = position;
		WaterfallPlane.Scale = scale;
		WaterfallPlane.Rotation = rotation;
		return new LongRunningAction(delegate
		{
			if (WaterfallPlane.Timing.Ended)
			{
				opening = false;
				SwapOpened();
				return true;
			}
			return false;
		});
	}

	private void SwapOpened()
	{
		LevelManager.RemovePlane(WaterfallPlane);
		LevelManager.RemovePlane(SplashPlane);
		Vector3 position = SplashPlane.Position;
		Vector3 scale = SplashPlane.Scale;
		Quaternion rotation = SplashPlane.Rotation;
		AnimatedTexture openSplash = OpenSplash;
		SplashPlane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, openSplash)
		{
			Doublesided = true
		};
		LevelManager.AddPlane(SplashPlane);
		SplashPlane.Timing.Restart();
		WaterfallPlane.Timing.Loop = true;
		SplashPlane.Position = position;
		SplashPlane.Scale = scale;
		SplashPlane.Rotation = rotation;
		position = WaterfallPlane.Position;
		scale = WaterfallPlane.Scale;
		rotation = WaterfallPlane.Rotation;
		openSplash = OpenWaterfall;
		WaterfallPlane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, openSplash)
		{
			Doublesided = true
		};
		LevelManager.AddPlane(WaterfallPlane);
		WaterfallPlane.Timing.Restart();
		WaterfallPlane.Timing.Loop = true;
		WaterfallPlane.YTextureRepeat = true;
		WaterfallPlane.Position = position;
		WaterfallPlane.Scale = scale;
		WaterfallPlane.Rotation = rotation;
	}
}
