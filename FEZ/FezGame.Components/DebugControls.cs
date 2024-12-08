using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

namespace FezGame.Components;

public class DebugControls : GameComponent
{
	private PolytronLogo pl;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ITimeService TimeService { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency(Optional = true)]
	public IMouseStateManager MouseState { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameService GameService { private get; set; }

	[ServiceDependency]
	public ISoundManager SM { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public IBlackHoleManager BlackHoles { private get; set; }

	public DebugControls(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		KeyboardState.RegisterKey(Keys.F1);
		KeyboardState.RegisterKey(Keys.F2);
		KeyboardState.RegisterKey(Keys.F3);
		KeyboardState.RegisterKey(Keys.F4);
		KeyboardState.RegisterKey(Keys.F5);
		KeyboardState.RegisterKey(Keys.F6);
		KeyboardState.RegisterKey(Keys.F8);
		KeyboardState.RegisterKey(Keys.F9);
		KeyboardState.RegisterKey(Keys.F10);
		KeyboardState.RegisterKey(Keys.F11);
		KeyboardState.RegisterKey(Keys.F12);
		KeyboardState.RegisterKey(Keys.NumPad0);
		KeyboardState.RegisterKey(Keys.NumPad1);
		KeyboardState.RegisterKey(Keys.NumPad2);
		KeyboardState.RegisterKey(Keys.NumPad3);
		KeyboardState.RegisterKey(Keys.NumPad4);
		KeyboardState.RegisterKey(Keys.NumPad5);
		KeyboardState.RegisterKey(Keys.NumPad6);
		KeyboardState.RegisterKey(Keys.NumPad7);
		KeyboardState.RegisterKey(Keys.NumPad8);
		KeyboardState.RegisterKey(Keys.NumPad9);
		KeyboardState.RegisterKey(Keys.D0);
		KeyboardState.RegisterKey(Keys.D1);
		KeyboardState.RegisterKey(Keys.D2);
		KeyboardState.RegisterKey(Keys.D3);
		KeyboardState.RegisterKey(Keys.D4);
		KeyboardState.RegisterKey(Keys.D5);
		KeyboardState.RegisterKey(Keys.D6);
		KeyboardState.RegisterKey(Keys.D7);
		KeyboardState.RegisterKey(Keys.D8);
		KeyboardState.RegisterKey(Keys.D9);
		KeyboardState.RegisterKey(Keys.L);
		KeyboardState.RegisterKey(Keys.H);
		KeyboardState.RegisterKey(Keys.J);
		KeyboardState.RegisterKey(Keys.K);
		KeyboardState.RegisterKey(Keys.R);
		KeyboardState.RegisterKey(Keys.T);
	}

	public override void Update(GameTime gameTime)
	{
		if (KeyboardState.GetKeyState(Keys.F1) == FezButtonState.Pressed)
		{
			GameState.DebugMode = true;
		}
		if (KeyboardState.GetKeyState(Keys.F2) == FezButtonState.Pressed)
		{
			GameState.DebugMode = false;
		}
		if (KeyboardState.GetKeyState(Keys.F3) == FezButtonState.Pressed)
		{
			SM.GlobalVolumeFactor = 0f;
		}
		if (KeyboardState.GetKeyState(Keys.F4) == FezButtonState.Pressed)
		{
			SM.GlobalVolumeFactor = 1f;
		}
		if (KeyboardState.GetKeyState(Keys.F5) == FezButtonState.Pressed)
		{
			GameState.ShowDebuggingBag = true;
		}
		if (KeyboardState.GetKeyState(Keys.F6) == FezButtonState.Pressed)
		{
			GameState.ShowDebuggingBag = false;
		}
		if (KeyboardState.GetKeyState(Keys.F9) == FezButtonState.Pressed)
		{
			TimeService.SetHour(4, immediate: true);
		}
		if (KeyboardState.GetKeyState(Keys.F10) == FezButtonState.Pressed)
		{
			TimeService.SetHour(12, immediate: true);
		}
		if (KeyboardState.GetKeyState(Keys.F11) == FezButtonState.Pressed)
		{
			TimeService.SetHour(20, immediate: true);
		}
		if (KeyboardState.GetKeyState(Keys.F12) == FezButtonState.Pressed)
		{
			TimeService.SetHour(0, immediate: true);
		}
		if (KeyboardState.GetKeyState(Keys.NumPad0) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D0) == FezButtonState.Pressed)
		{
			CameraManager.PixelsPerTrixel = 1f;
		}
		if (KeyboardState.GetKeyState(Keys.NumPad1) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D1) == FezButtonState.Pressed)
		{
			CameraManager.PixelsPerTrixel = 2f;
		}
		if (KeyboardState.GetKeyState(Keys.NumPad2) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D2) == FezButtonState.Pressed)
		{
			CameraManager.PixelsPerTrixel = 3f;
		}
		if (KeyboardState.GetKeyState(Keys.NumPad3) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D3) == FezButtonState.Pressed)
		{
			CameraManager.PixelsPerTrixel = 4f;
		}
		if (KeyboardState.GetKeyState(Keys.NumPad5) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D5) == FezButtonState.Pressed)
		{
			GameState.SaveData.CubeShards++;
			GameState.SaveData.ScoreDirty = true;
			GameState.OnHudElementChanged();
			ServiceHelper.Get<IGomezService>().OnCollectedShard();
		}
		if ((KeyboardState.GetKeyState(Keys.NumPad6) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D6) == FezButtonState.Pressed) && GameState.SaveData.CubeShards > 0)
		{
			GameState.SaveData.CubeShards--;
			GameState.SaveData.ScoreDirty = true;
			GameState.OnHudElementChanged();
		}
		if (KeyboardState.GetKeyState(Keys.NumPad7) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D7) == FezButtonState.Pressed)
		{
			GameState.SaveData.Keys++;
			GameState.OnHudElementChanged();
		}
		if ((KeyboardState.GetKeyState(Keys.NumPad8) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D8) == FezButtonState.Pressed) && GameState.SaveData.Keys > 0)
		{
			GameState.SaveData.Keys--;
			GameState.OnHudElementChanged();
		}
		if (KeyboardState.GetKeyState(Keys.NumPad9) == FezButtonState.Pressed || KeyboardState.GetKeyState(Keys.D9) == FezButtonState.Pressed)
		{
			GameState.SaveData.SecretCubes++;
			GameState.SaveData.ScoreDirty = true;
			GameState.OnHudElementChanged();
			ServiceHelper.Get<IGomezService>().OnCollectedAnti();
		}
		if (KeyboardState.GetKeyState(Keys.L) == FezButtonState.Pressed && LevelManager.Name == "TEMPLE_OF_LOVE")
		{
			GameState.SaveData.HasDoneHeartReboot = true;
		}
		if (KeyboardState.GetKeyState(Keys.LeftControl).IsDown() && KeyboardState.GetKeyState(Keys.S) == FezButtonState.Pressed)
		{
			GameState.SaveData.IsNew = false;
			GameState.Save();
		}
		if (KeyboardState.GetKeyState(Keys.H) == FezButtonState.Pressed)
		{
			BlackHoles.EnableAll();
		}
		if (KeyboardState.GetKeyState(Keys.J) == FezButtonState.Pressed)
		{
			BlackHoles.DisableAll();
		}
		if (KeyboardState.GetKeyState(Keys.K) == FezButtonState.Pressed)
		{
			BlackHoles.Randomize();
		}
		if (!Fez.LongScreenshot)
		{
			return;
		}
		if (KeyboardState.GetKeyState(Keys.R) == FezButtonState.Pressed)
		{
			SM.PlayNewSong(null);
			GameState.HideHUD = true;
			PlayerManager.Action = ActionType.StandWinking;
			CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView((!Fez.DoubleRotations) ? 1 : 2));
		}
		if (KeyboardState.GetKeyState(Keys.T) != FezButtonState.Pressed)
		{
			return;
		}
		SM.KillSounds(0.1f);
		SM.PlayNewSong(null, 0.1f);
		foreach (AmbienceTrack ambienceTrack in LevelManager.AmbienceTracks)
		{
			SM.MuteAmbience(ambienceTrack.Name, 0.1f);
		}
		if (pl != null)
		{
			ServiceHelper.RemoveComponent(pl);
		}
		pl = new PolytronLogo(base.Game)
		{
			DrawOrder = 10000,
			Opacity = 1f
		};
		ServiceHelper.AddComponent(pl);
		LogoRenderer tl = new LogoRenderer(base.Game)
		{
			DrawOrder = 9999,
			Visible = false,
			Enabled = false
		};
		ServiceHelper.AddComponent(tl);
		FezLogo FezLogo;
		ServiceHelper.AddComponent(FezLogo = new FezLogo(base.Game));
		SoundEffect soundEffect = CMProvider.Global.Load<SoundEffect>("Sounds/Intro/LogoZoom");
		FezLogo.Visible = true;
		FezLogo.Enabled = true;
		FezLogo.TransitionStarted = true;
		FezLogo.Opacity = 1f;
		FezLogo.Inverted = true;
		FezLogo.SinceStarted = 4.5f;
		FezLogo.HalfSpeed = true;
		FezLogo.Update(new GameTime());
		soundEffect.Emit();
		SoundManager.NoMoreSounds = true;
		GameState.SkipRendering = true;
		Waiters.Wait(7.0, delegate
		{
			FezLogo.Visible = false;
			tl.Visible = true;
			Waiters.Wait(1.5, delegate
			{
				pl.Visible = true;
				pl.Update(new GameTime());
				Waiters.Wait(0.5, delegate
				{
					pl.Enabled = true;
				});
			});
		});
	}
}
