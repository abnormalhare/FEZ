using System;
using Common;
using EasyStorage;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Components.Scripting;
using FezGame.Services;
using FezGame.Services.Scripting;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using Steamworks;

namespace FezGame;

public class Fez : Game
{
	public static bool LevelChooser;

	public static bool SkipIntro;

	public static bool TrialMode;

	public static bool SkipLogos;

	public static int ForceGoldenCubes;

	public static int ForceAntiCubes;

	public static bool LongScreenshot;

	public static bool PublicDemo;

	public static bool DoubleRotations;

	public static bool NoSteamworks;

	public static bool NoMusic;

	public static bool DisableSteamworksInit;

	public static bool SpeedRunMode;

	public static string ForcedLevelName = "GOMEZ_HOUSE_2D";

	public const string ForcedTrialLevelName = "trial/BIG_TOWER";

	private readonly GraphicsDeviceManager deviceManager;

	private InputManager inputManager;

	public static string Version = "1.12";

	private float sinceLoading;

	public bool IsDisposed { get; private set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	public Fez()
	{
		if (SDL.SDL_GetPlatform().Equals("Windows"))
		{
			try
			{
				ThreadExecutionState.SetUp();
			}
			catch (Exception ex)
			{
				Logger.Log("ThreadExecutionState", ex.ToString());
			}
		}
		Logger.Log("Version", Version + ", Build Date : " + LinkerTimestamp.BuildDateTime);
		if (SettingsManager.Settings.DisableSteamworks)
		{
			NoSteamworks = true;
			PCSaveDevice.DisableCloudSaves = true;
		}
		else
		{
			try
			{
				NoSteamworks = !SteamAPI.Init();
				if (!NoSteamworks)
				{
					SteamUserStats.RequestCurrentStats();
				}
			}
			catch (DllNotFoundException)
			{
				NoSteamworks = true;
			}
			if (NoSteamworks)
			{
				PCSaveDevice.DisableCloudSaves = true;
			}
		}
		deviceManager = new GraphicsDeviceManager(this);
		SettingsManager.DeviceManager = deviceManager;
		base.Content.RootDirectory = "Content";
		ServiceHelper.Game = this;
		ServiceHelper.IsFull = true;
	}

	public new void Exit()
	{
		base.Exit();
		if (!NoSteamworks)
		{
			SteamAPI.Shutdown();
		}
	}

	protected override void Initialize()
	{
		SettingsManager.InitializeResolutions();
		SettingsManager.InitializeCapabilities();
		base.IsFixedTimeStep = false;
		deviceManager.SynchronizeWithVerticalRetrace = true;
		SettingsManager.Apply();
		ServiceHelper.AddService(new KeyboardStateManager());
		ServiceHelper.AddService(new MouseStateManager());
		ServiceHelper.AddService(new PlayerManager());
		ServiceHelper.AddService(new CollisionManager());
		ServiceHelper.AddService(new DebuggingBag());
		ServiceHelper.AddService(new PhysicsManager());
		ServiceHelper.AddService(new TimeManager());
		ServiceHelper.AddService(new GameStateManager());
		ServiceHelper.AddComponent(new GamepadsManager(this, !SettingsManager.Settings.DisableController), addServices: true);
		ServiceHelper.AddComponent(inputManager = new InputManager(this, mouse: true, keyboard: true, !SettingsManager.Settings.DisableController), addServices: true);
		ServiceHelper.AddComponent(new ContentManagerProvider(this), addServices: true);
		ServiceHelper.AddComponent(new GameCameraManager(this), addServices: true);
		ServiceHelper.AddComponent(new GameLevelMaterializer(this), addServices: true);
		ServiceHelper.AddComponent(new GameLevelManager(this), addServices: true);
		ServiceHelper.AddComponent(new FogManager(this), addServices: true);
		ServiceHelper.AddComponent(new TargetRenderingManager(this), addServices: true);
		ServiceHelper.AddComponent(new SoundManager(this, NoMusic), addServices: true);
		ServiceHelper.AddComponent(new PersistentThreadPool(this), addServices: true);
		ServiceHelper.AddComponent(new TrixelParticleSystems(this), addServices: true);
		ServiceHelper.AddComponent(new TrialAndAwards(this), addServices: true);
		ServiceHelper.AddComponent(new SpeechBubble(this), addServices: true);
		ServiceHelper.AddComponent(new PlaneParticleSystems(this), addServices: true);
		ServiceHelper.AddComponent(new FontManager(this), addServices: true);
		ServiceHelper.AddComponent(new ScriptingHost(this), addServices: true);
		ServiceHelper.AddComponent(new DotHost(this), addServices: true);
		ServiceHelper.AddComponent(new RotatingGroupsHost(this), addServices: true);
		ServiceHelper.AddComponent(new BigWaterfallHost(this), addServices: true);
		ServiceHelper.AddComponent(new BlackHolesHost(this), addServices: true);
		ServiceHelper.AddService(new CameraService());
		ServiceHelper.AddService(new GomezService());
		ServiceHelper.AddService(new LevelService());
		ServiceHelper.AddService(new SoundService());
		ServiceHelper.AddService(new TimeService());
		ServiceHelper.AddService(new VolumeService());
		ServiceHelper.AddService(new ArtObjectService());
		ServiceHelper.AddService(new GroupService());
		ServiceHelper.AddService(new PlaneService());
		ServiceHelper.AddService(new NpcService());
		ServiceHelper.AddService(new ScriptService());
		ServiceHelper.AddService(new SwitchService());
		ServiceHelper.AddService(new BitDoorService());
		ServiceHelper.AddService(new SuckBlockService());
		ServiceHelper.AddService(new PathService());
		ServiceHelper.AddService(new SpinBlockService());
		ServiceHelper.AddService(new WarpGateService());
		ServiceHelper.AddService(new TombstoneService());
		ServiceHelper.AddService(new PivotService());
		ServiceHelper.AddService(new ValveService());
		ServiceHelper.AddService(new CodePatternService());
		ServiceHelper.AddService(new LaserEmitterService());
		ServiceHelper.AddService(new LaserReceiverService());
		ServiceHelper.AddService(new TimeswitchService());
		ServiceHelper.AddService(new DotService());
		ServiceHelper.AddService(new OwlService());
		ServiceHelper.InitializeServices();
		ServiceHelper.InjectServices(this);
		GameState.SaveData = new SaveData();
		base.Window.Title = "FEZ";
		if (SkipIntro)
		{
			LoadComponents(this);
			base.Initialize();
			GameState.SaveSlot = 0;
			GameState.SignInAndChooseStorage(Util.NullAction);
			GameState.LoadSaveFile(delegate
			{
				GameState.LoadLevelAsync(Util.NullAction);
			});
			GameState.SaveData.CanOpenMap = true;
			GameState.SaveData.IsNew = false;
		}
		else
		{
			ServiceHelper.AddComponent(new Intro(this));
			base.Initialize();
		}
	}

	internal static void LoadComponents(Fez game)
	{
		if (!ServiceHelper.FirstLoadDone)
		{
			ServiceHelper.AddComponent(new StaticPreloader(game));
			ServiceHelper.AddComponent(new GammaCorrection(game));
			ServiceHelper.AddComponent(new LoadingScreen(game));
			ServiceHelper.AddComponent(new TimeHost(game));
			ServiceHelper.AddComponent(new GameLevelHost(game));
			ServiceHelper.AddComponent(new PlayerCameraControl(game));
			ServiceHelper.AddComponent(new GameLightingPostProcess(game));
			ServiceHelper.AddComponent(new PlayerActions(game));
			ServiceHelper.AddComponent(new HeadsUpDisplay(game));
			ServiceHelper.AddComponent(new GomezHost(game));
			ServiceHelper.AddComponent(new VolumesHost(game));
			ServiceHelper.AddComponent(new GameStateControl(game));
			ServiceHelper.AddComponent(new SkyHost(game));
			ServiceHelper.AddComponent(new PickupsHost(game));
			ServiceHelper.AddComponent(new BombsHost(game));
			ServiceHelper.AddComponent(new GameSequencer(game));
			ServiceHelper.AddComponent(new BurnInPostProcess(game));
			ServiceHelper.AddComponent(new WatchersHost(game));
			ServiceHelper.AddComponent(new MovingGroupsHost(game));
			ServiceHelper.AddComponent(new AnimatedPlanesHost(game));
			ServiceHelper.AddComponent(new GameNpcHost(game));
			ServiceHelper.AddComponent(new PushSwitchesHost(game));
			ServiceHelper.AddComponent(new BitDoorsHost(game));
			ServiceHelper.AddComponent(new SuckBlocksHost(game));
			ServiceHelper.AddComponent(new LevelLooper(game));
			ServiceHelper.AddComponent(new CameraPathsHost(game));
			ServiceHelper.AddComponent(new WarpGateHost(game));
			ServiceHelper.AddComponent(new AttachedPlanesHost(game));
			ServiceHelper.AddComponent(new SpinBlocksHost(game));
			ServiceHelper.AddComponent(new PivotsHost(game));
			ServiceHelper.AddComponent(new SpinningTreasuresHost(game));
			ServiceHelper.AddComponent(new LiquidHost(game));
			ServiceHelper.AddComponent(new TombstonesHost(game));
			ServiceHelper.AddComponent(new SplitUpCubeHost(game));
			ServiceHelper.AddComponent(new ValvesBoltsTimeswitchesHost(game));
			ServiceHelper.AddComponent(new RumblerHost(game));
			ServiceHelper.AddComponent(new RainHost(game));
			ServiceHelper.AddComponent(new TempleOfLoveHost(game));
			ServiceHelper.AddComponent(new WaterfallsHost(game));
			ServiceHelper.AddComponent(new GeysersHost(game));
			ServiceHelper.AddComponent(new SewerLightHacks(game));
			ServiceHelper.AddComponent(new FarawayPlaceHost(game));
			ServiceHelper.AddComponent(new CodeMachineHost(game));
			ServiceHelper.AddComponent(new CrumblersHost(game));
			ServiceHelper.AddComponent(new MailboxesHost(game));
			ServiceHelper.AddComponent(new PointsOfInterestHost(game));
			ServiceHelper.AddComponent(new OrreryHost(game));
			ServiceHelper.AddComponent(new OwlStatueHost(game));
			ServiceHelper.AddComponent(new CryptHost(game));
			ServiceHelper.AddComponent(new BellHost(game));
			ServiceHelper.AddComponent(new TelescopeHost(game));
			ServiceHelper.AddComponent(new TetrisPuzzleHost(game));
			ServiceHelper.AddComponent(new SaveIndicator(game));
			ServiceHelper.AddComponent(new UnfoldPuzzleHost(game));
			ServiceHelper.AddComponent(new QrCodesHost(game));
			ServiceHelper.AddComponent(new GameWideCodes(game));
			ServiceHelper.AddComponent(new FirstPersonView(game));
			ServiceHelper.AddComponent(new Quantumizer(game));
			ServiceHelper.AddComponent(new NameOfGodPuzzleHost(game));
			ServiceHelper.AddComponent(new ClockTowerHost(game));
			ServiceHelper.AddComponent(new PyramidHost(game));
			ServiceHelper.AddComponent(new FinalRebuildHost(game));
			ServiceHelper.AddComponent(new SecretPassagesHost(game));
			ServiceHelper.AddComponent(new OwlHeadHost(game));
			ServiceHelper.AddComponent(new StargateHost(game));
			ServiceHelper.AddComponent(new FlickeringNeon(game));
			ServiceHelper.AddComponent(new GodRays(game));
			if (PublicDemo)
			{
				ServiceHelper.AddComponent(new IdleRestarter(game));
			}
			ServiceHelper.FirstLoadDone = true;
		}
	}

	protected override void Update(GameTime gameTime)
	{
		TimeInterpolation.NeedsInterpolation = true;
		TimeSpan totalGameTime = gameTime.TotalGameTime;
		TimeSpan timeSpan = totalGameTime - TimeInterpolation.LastUpdate;
		if (!(timeSpan < TimeInterpolation.UpdateTimestep))
		{
			double num;
			for (num = timeSpan.TotalMilliseconds; num > 17.0; num -= 17.0)
			{
				base.Update(new GameTime(totalGameTime, TimeInterpolation.UpdateTimestep));
			}
			TimeInterpolation.LastUpdate = totalGameTime.Subtract(TimeSpan.FromTicks((long)(num * 10000.0)));
			if (!NoSteamworks)
			{
				SteamAPI.RunCallbacks();
			}
		}
	}

	protected override void Draw(GameTime gameTime)
	{
		TimeInterpolation.ProcessCallbacks(gameTime);
		DrawActionScheduler.Process();
		if (GameState.ScheduleLoadEnd && (!GameState.DotLoading || sinceLoading > 5f))
		{
			IGameStateManager gameState = GameState;
			IGameStateManager gameState2 = GameState;
			bool flag2 = (GameState.DotLoading = false);
			bool scheduleLoadEnd = (gameState2.Loading = flag2);
			gameState.ScheduleLoadEnd = scheduleLoadEnd;
			ServiceHelper.FirstLoadDone = true;
			sinceLoading = 0f;
		}
		if (GameState.DotLoading)
		{
			sinceLoading += (float)gameTime.ElapsedGameTime.TotalSeconds;
		}
		if (inputManager.StrictRotation && !GameState.InMap)
		{
			inputManager.StrictRotation = false;
		}
		if (!GameState.Loading && !GameState.SkipRendering)
		{
			TargetRenderingManager.OnPreDraw(gameTime);
		}
		TargetRenderingManager.OnRtPrepare();
		base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
		base.GraphicsDevice.SetupViewport();
		base.GraphicsDevice.PrepareDraw();
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
		base.Draw(gameTime);
		SpeedRun.Draw((float)Math.Floor(base.GraphicsDevice.GetViewScale()));
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		OggStream.AbortPrecacher();
	}
}
