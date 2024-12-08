using System;
using System.Globalization;
using System.Threading;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class Intro : DrawableGameComponent
{
	private enum Screen
	{
		ESRB_PEGI,
		XBLA,
		MGS,
		WhiteScreen,
		Polytron,
		Trapdoor,
		TrixelEngine,
		SaveIndicator,
		Fez,
		SellScreen,
		Zoom,
		SignOutPrompt,
		SignInChooseDevice,
		MainMenu,
		Warp
	}

	private enum Phase
	{
		FadeIn,
		Wait,
		FadeOut
	}

	private Texture2D TrixelEngineText;

	private Texture2D TrapdoorLogo;

	private GlyphTextRenderer tr;

	private readonly Mesh TrixelPlanes = new Mesh
	{
		AlwaysOnTop = true,
		Blending = BlendingMode.Multiply,
		DepthWrites = false
	};

	private Mesh SaveIndicatorMesh;

	private Mesh TrialMesh;

	private FezLogo FezLogo;

	private PolytronLogo PolytronLogo;

	private IntroZoomIn IntroZoomIn;

	private IntroPanDown IntroPanDown;

	private MainMenu MainMenu;

	private SoundEffect sTitleBassHit;

	private SoundEffect sTrixelIn;

	private SoundEffect sTrixelOut;

	private SoundEffect sExitGame;

	private SoundEffect sConfirm;

	private SoundEffect sDrone;

	private SoundEffect sStarZoom;

	private SoundEmitter eDrone;

	private Phase phase;

	private Screen screen;

	private SpriteBatch spriteBatch;

	private TimeSpan phaseTime;

	private bool scheduledBackToGame;

	private bool ZoomToHouse;

	private static bool PreloadStarted;

	private static bool PreloadComplete;

	private static bool FirstBootComplete;

	private float opacity;

	private float promptOpacity;

	private static StarField Starfield;

	private static bool HasShownSaveIndicator;

	private static bool firstDrawDone;

	private float dotdotdot;

	private bool didPanDown;

	public static Intro Instance { get; private set; }

	public bool Glitch { get; set; }

	public bool Fake { get; set; }

	public bool Sell { get; set; }

	public bool FadeBackToGame { get; set; }

	public bool Restarted { get; set; }

	public bool NewSaveSlot { get; set; }

	public string FakeLevel { get; set; }

	public bool FullLogos { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	[ServiceDependency]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public IFontManager Fonts { private get; set; }

	[ServiceDependency]
	public IGameService GameService { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public Intro(Game game)
		: base(game)
	{
		base.DrawOrder = 2005;
		base.UpdateOrder = -1;
		Instance = this;
	}

	protected override void LoadContent()
	{
		ContentManager IntroCM = CMProvider.Get(CM.Intro);
		StaticText.GetString("Loading");
		bool is1440 = base.GraphicsDevice.GetViewScale() >= 1.5f;
		DrawActionScheduler.Schedule(delegate
		{
			TrixelEngineText = IntroCM.Load<Texture2D>("Other Textures/splash/trixels" + (is1440 ? "_1440" : ""));
			TrapdoorLogo = IntroCM.Load<Texture2D>("Other Textures/splash/trapdoor");
			spriteBatch = new SpriteBatch(base.GraphicsDevice);
		});
		tr = new GlyphTextRenderer(base.Game);
		tr.IgnoreKeyboardRemapping = true;
		KeyboardState.IgnoreMapping = true;
		TrixelPlanes.Position = (Vector3.Right + Vector3.Up) * -0.125f - Vector3.Up * 0.25f;
		TrixelPlanes.Scale = new Vector3(0.75f);
		TrixelPlanes.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Back, Color.Magenta, centeredOnOrigin: true, doublesided: false);
		TrixelPlanes.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Top, Color.Yellow, centeredOnOrigin: true, doublesided: false);
		TrixelPlanes.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Left, Color.Cyan, centeredOnOrigin: true, doublesided: false);
		base.GraphicsDevice.SetupViewport();
		float aspectRatio = base.GraphicsDevice.Viewport.AspectRatio;
		TrialMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false
		};
		TrialMesh.AddColoredBox(Vector3.One, Vector3.Zero, new Color(209, 0, 55), centeredOnOrigin: true);
		DrawActionScheduler.Schedule(delegate
		{
			TrixelPlanes.Effect = new DefaultEffect.VertexColored
			{
				ForcedProjectionMatrix = Matrix.CreateOrthographic(2f * aspectRatio, 2f, 0.1f, 100f),
				ForcedViewMatrix = Matrix.CreateLookAt(Vector3.UnitY - Vector3.UnitZ - Vector3.UnitX, Vector3.Zero, Vector3.Up)
			};
			TrialMesh.Effect = new DefaultEffect.VertexColored
			{
				ForcedProjectionMatrix = Matrix.CreateOrthographic(5f * aspectRatio, 5f, 0.1f, 100f),
				ForcedViewMatrix = Matrix.CreateLookAt(Vector3.UnitY - Vector3.UnitZ - Vector3.UnitX, Vector3.Zero, Vector3.Up)
			};
		});
		ServiceHelper.AddComponent(PolytronLogo = new PolytronLogo(base.Game));
		screen = Screen.WhiteScreen;
		FirstBootComplete = true;
		if (Restarted && !FullLogos)
		{
			screen = Screen.Fez;
		}
		if (GameState.ForcedSignOut)
		{
			InputManager.ClearActiveController();
			screen = Screen.SignInChooseDevice;
			ServiceHelper.AddComponent(Starfield = new StarField(base.Game));
		}
		if (GameState.LoggedOutPlayerTag != null)
		{
			InputManager.ClearActiveController();
			screen = Screen.SignOutPrompt;
			ServiceHelper.AddComponent(Starfield = new StarField(base.Game));
		}
		if (Fez.SkipLogos)
		{
			screen = Screen.Fez;
		}
		if (Fake)
		{
			screen = Screen.Polytron;
			PolytronLogo.Enabled = true;
		}
		if (Sell)
		{
			SoundManager.MuteAmbienceTracks();
			SoundManager.PlayNewSong("GOMEZ", 0.1f);
			screen = Screen.SellScreen;
		}
		GameState.ForceTimePaused = true;
		GameState.InCutscene = true;
		phaseTime = TimeSpan.FromSeconds(-0.6000000238418579);
		sTitleBassHit = IntroCM.Load<SoundEffect>("Sounds/Intro/LogoZoom");
		sTrixelIn = IntroCM.Load<SoundEffect>("Sounds/Intro/TrixelLogoIn");
		sTrixelOut = IntroCM.Load<SoundEffect>("Sounds/Intro/TrixelLogoOut");
		sExitGame = IntroCM.Load<SoundEffect>("Sounds/Ui/Menu/ExitGame");
		sConfirm = IntroCM.Load<SoundEffect>("Sounds/Ui/Menu/Confirm");
		sDrone = IntroCM.Load<SoundEffect>("Sounds/Intro/FezLogoDrone");
		sStarZoom = IntroCM.Load<SoundEffect>("Sounds/Intro/StarZoom");
		ServiceHelper.AddComponent(FezLogo = new FezLogo(base.Game)
		{
			Glitched = Glitch
		});
		if (Sell)
		{
			FezLogo.Inverted = true;
			FezLogo.TransitionStarted = true;
			FezLogo.LogoTextureXFade = 1f;
			FezLogo.Opacity = 1f;
			Starfield = FezLogo.Starfield;
			bool enabled = (base.Visible = false);
			base.Enabled = enabled;
		}
		if (!HasShownSaveIndicator)
		{
			float viewAspect = base.GraphicsDevice.Viewport.AspectRatio;
			SaveIndicatorMesh = new Mesh
			{
				Blending = BlendingMode.Alphablending,
				AlwaysOnTop = true,
				DepthWrites = false
			};
			SaveIndicatorMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, Color.Red, centeredOnOrigin: true);
			DrawActionScheduler.Schedule(delegate
			{
				SaveIndicatorMesh.Effect = new DefaultEffect.VertexColored
				{
					ForcedProjectionMatrix = Matrix.CreateOrthographic(14f * viewAspect, 14f, 0.1f, 100f),
					ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
				};
			});
		}
	}

	private void Kill()
	{
		ServiceHelper.RemoveComponent(this);
	}

	public void LoadVideo()
	{
		bool enabled = (base.Visible = true);
		base.Enabled = enabled;
	}

	protected override void Dispose(bool disposing)
	{
		KeyboardState.IgnoreMapping = false;
		if (CMProvider != null)
		{
			CMProvider.Dispose(CM.Intro);
			GameState.DynamicUpgrade -= Kill;
			Instance = null;
			if (FezLogo != null)
			{
				ServiceHelper.RemoveComponent(FezLogo);
			}
			if (PolytronLogo != null)
			{
				ServiceHelper.RemoveComponent(PolytronLogo);
				PolytronLogo = null;
			}
			if (IntroPanDown != null && !IntroPanDown.IsDisposed)
			{
				ServiceHelper.RemoveComponent(IntroPanDown);
			}
			if (IntroZoomIn != null && !IntroZoomIn.IsDisposed)
			{
				ServiceHelper.RemoveComponent(IntroZoomIn);
			}
		}
		if (Starfield != null && Starfield.IsDisposed)
		{
			Starfield = null;
		}
		TrixelPlanes.Dispose();
		TrialMesh.Dispose();
		if (SaveIndicatorMesh != null)
		{
			SaveIndicatorMesh.Dispose();
		}
		base.Dispose(disposing);
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused)
		{
			return;
		}
		phaseTime += gameTime.ElapsedGameTime;
		if (screen < Screen.Fez && screen != 0 && (screen != Screen.SaveIndicator || !SettingsManager.FirstOpen) && InputManager.AnyButtonPressed() && ServiceHelper.FirstLoadDone)
		{
			if (phase == Phase.FadeIn)
			{
				ChangePhase();
			}
			ChangePhase();
		}
		UpdateLogo();
	}

	private void DoPreLoad(bool dummy)
	{
		Logger.Try(Fez.LoadComponents, base.Game as Fez);
		PreloadComplete = true;
		Logger.Log("Intro", "Preloading complete.");
	}

	private void UpdateLogo()
	{
		double totalSeconds = phaseTime.TotalSeconds;
		switch (screen)
		{
		case Screen.ESRB_PEGI:
			switch (phase)
			{
			case Phase.FadeIn:
				opacity = 1f;
				ChangePhase();
				break;
			case Phase.Wait:
				if (totalSeconds >= 4.0)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 0.25)
				{
					ChangePhase();
				}
				opacity = 1f - Easing.EaseIn(FezMath.Saturate(totalSeconds * 4.0), EasingType.Sine);
				break;
			}
			break;
		case Screen.XBLA:
			switch (phase)
			{
			case Phase.FadeIn:
				if (totalSeconds >= 0.125)
				{
					ChangePhase();
				}
				opacity = Easing.EaseIn(FezMath.Saturate(totalSeconds * 8.0), EasingType.Sine);
				break;
			case Phase.Wait:
				if (totalSeconds >= 2.0)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 0.25)
				{
					ChangePhase();
				}
				opacity = 1f - Easing.EaseIn(FezMath.Saturate(totalSeconds * 4.0), EasingType.Sine);
				break;
			}
			break;
		case Screen.MGS:
			switch (phase)
			{
			case Phase.FadeIn:
				if (totalSeconds >= 0.125)
				{
					ChangePhase();
				}
				opacity = Easing.EaseIn(FezMath.Saturate(totalSeconds * 8.0), EasingType.Sine);
				break;
			case Phase.Wait:
				if (totalSeconds >= 2.0)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 0.25)
				{
					ChangePhase();
				}
				opacity = Math.Min(opacity, 1f - Easing.EaseIn(FezMath.Saturate(totalSeconds * 4.0), EasingType.Sine));
				break;
			}
			break;
		case Screen.WhiteScreen:
			switch (phase)
			{
			case Phase.FadeIn:
				if (totalSeconds >= 0.125)
				{
					ChangePhase();
				}
				opacity = Easing.EaseIn(FezMath.Saturate(totalSeconds * 8.0), EasingType.Sine);
				break;
			case Phase.Wait:
				if (!PreloadStarted)
				{
					GameState.SkipLoadScreen = true;
					GameState.Loading = true;
					Worker<bool> worker = ThreadPool.Take<bool>(DoPreLoad);
					worker.Priority = ThreadPriority.AboveNormal;
					worker.Finished += delegate
					{
						ThreadPool.Return(worker);
						GameState.ScheduleLoadEnd = true;
						GameState.SkipLoadScreen = false;
					};
					worker.Start(context: false);
					PreloadStarted = true;
				}
				if (PreloadComplete)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 0.25)
				{
					ChangePhase();
				}
				opacity = Math.Min(opacity, 1f - Easing.EaseIn(FezMath.Saturate(totalSeconds * 4.0), EasingType.Sine));
				break;
			}
			break;
		case Screen.Polytron:
			switch (phase)
			{
			case Phase.FadeIn:
				if (totalSeconds >= 1.5)
				{
					ChangePhase();
				}
				opacity = 0f;
				PolytronLogo.Opacity = 1f;
				break;
			case Phase.Wait:
				if (totalSeconds >= 1.5)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 0.25)
				{
					ChangePhase();
				}
				PolytronLogo.Opacity = 1f - Easing.EaseIn(FezMath.Saturate(totalSeconds * 4.0), EasingType.Sine);
				break;
			}
			break;
		case Screen.Trapdoor:
			switch (phase)
			{
			case Phase.FadeIn:
				if (totalSeconds >= 0.125)
				{
					ChangePhase();
				}
				opacity = Easing.EaseIn(FezMath.Saturate(totalSeconds * 8.0), EasingType.Sine);
				break;
			case Phase.Wait:
				if (totalSeconds >= 1.5)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 0.25)
				{
					ChangePhase();
				}
				opacity = Math.Min(opacity, 1f - Easing.EaseIn(FezMath.Saturate(totalSeconds * 4.0), EasingType.Sine));
				break;
			}
			break;
		case Screen.TrixelEngine:
		{
			float num = 0f;
			switch (phase)
			{
			case Phase.FadeIn:
				if (sTrixelIn != null)
				{
					sTrixelIn.Emit();
					sTrixelIn = null;
				}
				if (totalSeconds >= 1.0)
				{
					ChangePhase();
				}
				num = (1f - Easing.EaseOut(FezMath.Saturate(totalSeconds), EasingType.Quadratic)) * 6f;
				opacity = Easing.EaseIn(FezMath.Saturate(totalSeconds * 2.0 - 0.5), EasingType.Quadratic);
				break;
			case Phase.Wait:
				if (totalSeconds >= 1.0)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (sTrixelOut != null)
				{
					sTrixelOut.Emit();
					sTrixelOut = null;
				}
				if (totalSeconds >= 0.5)
				{
					ChangePhase();
				}
				opacity = Math.Min(opacity, 1f - Easing.EaseIn(FezMath.Saturate(totalSeconds * 2.0), EasingType.Quadratic));
				num = Easing.EaseIn(FezMath.Saturate(totalSeconds * 2.0), EasingType.Quadratic) * -4f;
				break;
			}
			TrixelPlanes.Groups[0].Position = Vector3.Right * (0.5f + num);
			TrixelPlanes.Groups[1].Position = Vector3.Up * (0.5f + num);
			TrixelPlanes.Groups[2].Position = Vector3.Backward * num;
			break;
		}
		case Screen.SaveIndicator:
			switch (phase)
			{
			case Phase.FadeIn:
				eDrone.VolumeFactor = FezMath.Saturate((float)totalSeconds / 0.125f);
				opacity = Easing.EaseIn(FezMath.Saturate(totalSeconds / 0.125), EasingType.Sine);
				if (totalSeconds >= 0.125)
				{
					ChangePhase();
				}
				break;
			case Phase.Wait:
				if (totalSeconds >= 2.625)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				opacity = Math.Min(opacity, 1f - Easing.EaseOut(FezMath.Saturate(totalSeconds / 0.25), EasingType.Sine));
				eDrone.VolumeFactor = 1f - FezMath.Saturate((float)totalSeconds / 0.25f) * 0.5f;
				if (totalSeconds >= 0.25)
				{
					ChangePhase();
				}
				break;
			}
			break;
		case Screen.Fez:
			switch (phase)
			{
			case Phase.FadeIn:
				if (totalSeconds >= 0.75)
				{
					ChangePhase();
				}
				opacity = Easing.EaseInOut(FezMath.Saturate(totalSeconds / 0.75), EasingType.Sine);
				promptOpacity = Easing.EaseIn(FezMath.Saturate((totalSeconds - 0.25) / 0.5), EasingType.Sine);
				break;
			case Phase.Wait:
				opacity = (promptOpacity = 1f);
				if (InputManager.Jump == FezButtonState.Pressed || InputManager.Start == FezButtonState.Pressed)
				{
					if (!Fake)
					{
						InputManager.DetermineActiveController();
					}
					SoundManager.PlayNewSong(null, 0f);
					FezLogo.TransitionStarted = true;
					sTitleBassHit.Emit().Persistent = true;
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				promptOpacity = (opacity = 1f - Easing.EaseOut(FezMath.Saturate(totalSeconds / 1.0), EasingType.Sine));
				if (totalSeconds >= 1.0)
				{
					ChangePhase();
				}
				break;
			}
			break;
		case Screen.SellScreen:
			switch (phase)
			{
			case Phase.FadeIn:
				GameState.SkipRendering = true;
				opacity = Easing.EaseInOut(FezMath.Saturate(totalSeconds - 14.0), EasingType.Sine);
				if (totalSeconds >= 15.0)
				{
					ChangePhase();
				}
				break;
			case Phase.Wait:
				FezLogo.LogoTextureXFade = 1f - Easing.EaseIn(FezMath.Saturate(((float)totalSeconds - 76f) / 4f), EasingType.Sine);
				if (InputManager.Jump == FezButtonState.Pressed || InputManager.Start == FezButtonState.Pressed)
				{
					if (Fez.PublicDemo)
					{
						GameState.Restart();
						break;
					}
					sExitGame.Emit();
					GameState.ReturnToArcade();
					base.Enabled = false;
				}
				else if (!Fez.PublicDemo && (InputManager.CancelTalk == FezButtonState.Pressed || InputManager.Back == FezButtonState.Pressed))
				{
					GameState.SkipRendering = false;
					FezLogo.Inverted = false;
					if (!FadeBackToGame)
					{
						sTitleBassHit.Emit();
					}
					else
					{
						SoundManager.PlayNewSong(null, 0.5f);
						SoundManager.UnshelfSong();
						SoundManager.Resume();
						SoundManager.UnmuteAmbienceTracks(apply: true);
						scheduledBackToGame = true;
						FezLogo.DoubleTime = true;
						PlayerManager.Hidden = true;
						GameState.InCutscene = false;
						GameState.ForceTimePaused = false;
					}
					ChangePhase();
				}
				else if (!Fez.PublicDemo && InputManager.GrabThrow == FezButtonState.Pressed)
				{
					sConfirm.Emit();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 2.0)
				{
					if (scheduledBackToGame)
					{
						PlayerManager.Hidden = false;
						PlayerManager.Ground = default(MultipleHits<TrileInstance>);
						PlayerManager.Velocity = 3.15f * (float)Math.Sign(CollisionManager.GravityFactor) * 0.15f * (1f / 60f) * -Vector3.UnitY;
						PhysicsManager.Update(PlayerManager);
						PlayerManager.Velocity = 3.15f * (float)Math.Sign(CollisionManager.GravityFactor) * 0.15f * (1f / 60f) * -Vector3.UnitY;
						PlayerManager.Action = ActionType.ExitDoor;
						ServiceHelper.RemoveComponent(this);
					}
					else
					{
						ChangePhase();
					}
				}
				opacity = 1f - Easing.EaseInOut(FezMath.Saturate(totalSeconds / 2.0), EasingType.Sine);
				break;
			}
			break;
		case Screen.Zoom:
			switch (phase)
			{
			case Phase.FadeIn:
				ChangePhase();
				break;
			case Phase.Wait:
				if (FezLogo.IsFullscreen)
				{
					Starfield = FezLogo.Starfield;
					ServiceHelper.RemoveComponent(FezLogo);
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				ChangePhase();
				break;
			}
			break;
		case Screen.SignOutPrompt:
			switch (phase)
			{
			case Phase.FadeIn:
				if (totalSeconds >= 0.5)
				{
					ChangePhase();
				}
				opacity = FezMath.Saturate((float)totalSeconds * 2f);
				break;
			case Phase.Wait:
				if (InputManager.Jump == FezButtonState.Pressed || InputManager.Start == FezButtonState.Pressed)
				{
					InputManager.DetermineActiveController();
					FezLogo.TransitionStarted = true;
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (totalSeconds >= 0.5)
				{
					ChangePhase();
				}
				opacity = 1f - FezMath.Saturate((float)totalSeconds * 2f);
				break;
			}
			break;
		case Screen.SignInChooseDevice:
			switch (phase)
			{
			case Phase.FadeIn:
				if (Fake)
				{
					ChangePhase();
				}
				else
				{
					GameState.SignInAndChooseStorage(ChangePhase);
				}
				ChangePhase();
				break;
			case Phase.FadeOut:
				ChangePhase();
				break;
			case Phase.Wait:
				break;
			}
			break;
		case Screen.MainMenu:
			switch (phase)
			{
			case Phase.FadeIn:
				GameState.ForcedSignOut = false;
				if (!Fake)
				{
					if (!GameState.SaveData.HasNewGamePlus || GameState.SaveData.Level != null)
					{
						GameState.LoadSaveFile(delegate
						{
							Waiters.Wait(0.0, delegate
							{
								base.Enabled = true;
								ServiceHelper.AddComponent(MainMenu = new MainMenu(base.Game));
								ChangePhase();
							});
						});
						base.Enabled = false;
					}
					else
					{
						ServiceHelper.AddComponent(MainMenu = new MainMenu(base.Game));
						ChangePhase();
					}
				}
				else
				{
					ChangePhase();
				}
				break;
			case Phase.Wait:
				if (Fake || MainMenu.StartedGame)
				{
					ZoomToHouse = true;
					Starfield.Enabled = true;
					sStarZoom.Emit().Persistent = true;
					StartLoading();
					ChangePhase();
				}
				else if (MainMenu.HasBought)
				{
					GameState.ClearSaveFile();
					screen = Screen.SignInChooseDevice;
					phase = Phase.FadeIn;
				}
				else if (MainMenu.SellingTime)
				{
					GameService.EndTrial(forceRestart: true);
					ServiceHelper.RemoveComponent(MainMenu);
					MainMenu = null;
					base.Enabled = false;
				}
				else if (MainMenu.ContinuedGame)
				{
					StartLoading();
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				ChangePhase();
				break;
			}
			break;
		case Screen.Warp:
			switch (phase)
			{
			case Phase.FadeIn:
				if (!GameState.Loading)
				{
					ChangePhase();
				}
				break;
			case Phase.Wait:
				if (GameState.IsTrialMode)
				{
					ServiceHelper.AddComponent(new TileTransition(ServiceHelper.Game)
					{
						ScreenCaptured = delegate
						{
							ServiceHelper.RemoveComponent(this);
							GameState.InCutscene = false;
							GameState.ForceTimePaused = false;
						}
					});
					ChangePhase();
				}
				if (ZoomToHouse && Starfield != null && Starfield.IsDisposed && IntroZoomIn == null)
				{
					ServiceHelper.AddComponent(IntroZoomIn = new IntroZoomIn(base.Game));
				}
				if (ZoomToHouse && IntroZoomIn != null && IntroZoomIn.IsDisposed)
				{
					IntroZoomIn = null;
					GameState.SaveData.CanNewGamePlus = false;
					Logger.Log("Intro", "Intro is done and game is go!");
					ServiceHelper.RemoveComponent(this);
				}
				if (!GameState.IsTrialMode && !ZoomToHouse && IntroPanDown != null && !IntroPanDown.DoPanDown)
				{
					ChangePhase();
				}
				break;
			case Phase.FadeOut:
				if (!GameState.IsTrialMode && (IntroPanDown == null || IntroPanDown.IsDisposed))
				{
					IntroPanDown = null;
					Logger.Log("Intro", "Intro is done and game is go!");
					ServiceHelper.RemoveComponent(this);
				}
				break;
			}
			break;
		}
	}

	private void ChangePhase()
	{
		switch (phase)
		{
		case Phase.FadeIn:
			phase = Phase.Wait;
			break;
		case Phase.Wait:
			phase = Phase.FadeOut;
			break;
		case Phase.FadeOut:
			phase = Phase.FadeIn;
			ChangeScreen();
			break;
		}
		phaseTime = TimeSpan.Zero;
	}

	private void ChangeScreen()
	{
		switch (screen)
		{
		case Screen.ESRB_PEGI:
			screen = Screen.XBLA;
			break;
		case Screen.XBLA:
			screen = Screen.MGS;
			break;
		case Screen.MGS:
			screen = Screen.WhiteScreen;
			break;
		case Screen.WhiteScreen:
			screen = Screen.Polytron;
			PolytronLogo.Enabled = true;
			break;
		case Screen.Polytron:
			PolytronLogo.Enabled = false;
			PolytronLogo.End();
			screen = Screen.Trapdoor;
			break;
		case Screen.Trapdoor:
			screen = Screen.TrixelEngine;
			break;
		case Screen.TrixelEngine:
			if (HasShownSaveIndicator || Fake)
			{
				screen = Screen.Fez;
				eDrone = sDrone.Emit(loop: true, 0f, 0.5f);
			}
			else
			{
				screen = Screen.SaveIndicator;
				eDrone = sDrone.Emit(loop: true, 0f, 0f);
				HasShownSaveIndicator = true;
			}
			break;
		case Screen.SaveIndicator:
			screen = Screen.Fez;
			SoundManager.PlayNewSong("FEZ", 1f);
			if (eDrone != null)
			{
				eDrone.FadeOutAndDie(3f, autoPause: false);
			}
			break;
		case Screen.Fez:
			screen = Screen.Zoom;
			if (Glitch)
			{
				eDrone.FadeOutAndDie(3f, autoPause: false);
			}
			break;
		case Screen.SellScreen:
			screen = Screen.Zoom;
			break;
		case Screen.Zoom:
			screen = Screen.SignInChooseDevice;
			break;
		case Screen.SignOutPrompt:
			GameState.LoggedOutPlayerTag = null;
			screen = Screen.SignInChooseDevice;
			break;
		case Screen.SignInChooseDevice:
			screen = Screen.MainMenu;
			break;
		case Screen.MainMenu:
			screen = Screen.Warp;
			break;
		}
	}

	private void StartLoading()
	{
		GameState.SkipLoadBackground = true;
		GameState.Loading = true;
		Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
		worker.Priority = ThreadPriority.Normal;
		worker.Finished += delegate
		{
			ThreadPool.Return(worker);
		};
		worker.Start(context: false);
	}

	private void DoLoad(bool dummy)
	{
		Logger.Try(DoLoad);
	}

	private void DoLoad()
	{
		if (!Fake)
		{
			GameState.LoadLevel();
		}
		else
		{
			LevelManager.ChangeLevel(FakeLevel);
		}
		Logger.Log("Intro", "Level load complete.");
		if (!ZoomToHouse)
		{
			ServiceHelper.AddComponent(IntroPanDown = new IntroPanDown(base.Game));
		}
		GameState.ScheduleLoadEnd = true;
		GameState.SkipLoadBackground = false;
	}

	public override void Draw(GameTime gameTime)
	{
		if (Fez.SkipLogos)
		{
			return;
		}
		float viewScale = base.GraphicsDevice.GetViewScale();
		if (!firstDrawDone)
		{
			Logger.Log("Intro", "First draw done!");
			firstDrawDone = true;
		}
		Vector2 vector = new Vector2(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height);
		Vector2 vector2 = (vector / 2f).Round();
		if (screen < Screen.SignOutPrompt)
		{
			if (screen == Screen.SellScreen && phase == Phase.FadeOut && scheduledBackToGame)
			{
				TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, opacity));
			}
			else if (screen == Screen.WhiteScreen)
			{
				base.GraphicsDevice.Clear(Color.White);
			}
			else
			{
				TargetRenderer.DrawFullscreen(Color.White);
			}
		}
		switch (screen)
		{
		case Screen.WhiteScreen:
		{
			spriteBatch.BeginPoint();
			dotdotdot += (float)gameTime.ElapsedGameTime.TotalSeconds * 2f;
			string @string = StaticText.GetString("Loading");
			@string = @string.Substring(0, @string.Length - 3);
			for (int i = 0; i < (int)dotdotdot % 4; i++)
			{
				@string += ".";
			}
			tr.DrawString(spriteBatch, Fonts.Small, @string.ToUpper(CultureInfo.InvariantCulture), new Vector2(50f, (float)base.GraphicsDevice.Viewport.Height - 65f * ((1f + viewScale) / 2f)), new Color(0f, 0f, 0f, opacity), Fonts.SmallFactor * viewScale);
			string text = "v" + Fez.Version;
			Vector2 vector3 = Fonts.Small.MeasureString(text) * Fonts.SmallFactor * viewScale;
			tr.DrawString(spriteBatch, Fonts.Small, text, new Vector2((float)(base.GraphicsDevice.Viewport.Width - 50) - vector3.X, (float)base.GraphicsDevice.Viewport.Height - 65f * ((1f + viewScale) / 2f)), new Color(0f, 0f, 0f, opacity), Fonts.SmallFactor * viewScale);
			spriteBatch.End();
			break;
		}
		case Screen.Trapdoor:
			spriteBatch.BeginPoint();
			spriteBatch.Draw(TrapdoorLogo, vector2 - (new Vector2(TrapdoorLogo.Width, TrapdoorLogo.Height) / 2f).Round(), new Color(1f, 1f, 1f, opacity));
			spriteBatch.End();
			break;
		case Screen.Polytron:
			PolytronLogo.Draw(gameTime);
			break;
		case Screen.TrixelEngine:
			if (opacity != 0f)
			{
				TrixelPlanes.Position = TrixelPlanes.Position * new Vector3(1f, 0f, 1f) + -0.375f * Vector3.UnitY;
				TrixelPlanes.Scale = new Vector3(0.75f);
				TrixelPlanes.Draw();
			}
			spriteBatch.BeginPoint();
			spriteBatch.Draw(TrixelEngineText, (new Vector2(vector2.X, vector2.Y / 1.8f) - new Vector2(TrixelEngineText.Width, TrixelEngineText.Height) / 2f).Round(), new Color(1f, 1f, 1f, opacity));
			spriteBatch.End();
			break;
		case Screen.SaveIndicator:
			SaveIndicatorMesh.Material.Opacity = opacity;
			SaveIndicatorMesh.FirstGroup.Position = new Vector3(0f, 2.5f, 0f);
			SaveIndicatorMesh.FirstGroup.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (0f - (float)gameTime.ElapsedGameTime.TotalSeconds) * 3f) * SaveIndicatorMesh.FirstGroup.Rotation;
			SaveIndicatorMesh.Draw();
			spriteBatch.BeginPoint();
			tr.DrawCenteredString(spriteBatch, Fonts.Big, StaticText.GetString("SaveIndicator"), new Color(0f, 0f, 0f, opacity), new Vector2(0f, vector.Y * 0.425f), Fonts.BigFactor * viewScale);
			spriteBatch.End();
			break;
		case Screen.SellScreen:
		{
			if (phase == Phase.FadeOut)
			{
				FezLogo.LogoTextureXFade = Math.Min(opacity, FezLogo.LogoTextureXFade);
				if (scheduledBackToGame)
				{
					FezLogo.Opacity = opacity;
				}
			}
			FezLogo.Draw(gameTime);
			if (Culture.IsCJK)
			{
				spriteBatch.BeginLinear();
			}
			else
			{
				spriteBatch.BeginPoint();
			}
			float num = Fonts.BigFactor + 0.25f;
			if (!Culture.IsCJK)
			{
				num += 0.75f;
			}
			tr.DrawString(spriteBatch, Fonts.Big, StaticText.GetString("SellTitle"), new Vector2(334f, 50f), new Color(0f, 0f, 0f, opacity), num);
			if (Fez.PublicDemo)
			{
				tr.DrawStringLFLeftAlign(spriteBatch, Fonts.Small, StaticText.GetString("SellButtonsPublicDemo"), new Color(0f, 0f, 0f, opacity), new Vector2(953f, 683f), Fonts.SmallFactor);
			}
			else
			{
				tr.DrawStringLFLeftAlign(spriteBatch, Fonts.Small, StaticText.GetString(FadeBackToGame ? "SellButtonsEndTrial" : "SellButtons"), new Color(0f, 0f, 0f, opacity), new Vector2(953f, 683f), Fonts.SmallFactor);
			}
			spriteBatch.End();
			break;
		}
		case Screen.Fez:
		case Screen.Zoom:
		{
			if (!FezLogo.IsDisposed)
			{
				if (!FezLogo.TransitionStarted)
				{
					FezLogo.Opacity = opacity;
				}
				FezLogo.Draw(gameTime);
			}
			else
			{
				TargetRenderer.DrawFullscreen(Color.Black);
				Starfield.Draw();
			}
			if (Culture.IsCJK)
			{
				spriteBatch.BeginLinear();
			}
			else
			{
				spriteBatch.BeginPoint();
			}
			string text2 = StaticText.GetString("SplashStart");
			if (!GamepadState.AnyConnected)
			{
				text2 = text2.Replace("{A}", "{START}");
			}
			tr.DrawCenteredString(spriteBatch, Fonts.Big, text2, new Color(0f, 0f, 0f, promptOpacity), new Vector2(150f * viewScale, vector.Y * 4f / 5f), Fonts.BigFactor * viewScale);
			spriteBatch.End();
			break;
		}
		case Screen.SignOutPrompt:
			TargetRenderer.DrawFullscreen(Color.Black);
			Starfield.Draw();
			spriteBatch.BeginPoint();
			tr.DrawCenteredString(spriteBatch, Fonts.Big, StaticText.GetString("SignOutNotice"), new Color(1f, 1f, 1f, opacity), new Vector2(0f, vector.Y / 3f), Fonts.BigFactor);
			spriteBatch.End();
			break;
		case Screen.SignInChooseDevice:
		case Screen.MainMenu:
		case Screen.Warp:
			if (IntroZoomIn == null && (IntroPanDown == null || !IntroPanDown.DoPanDown))
			{
				TargetRenderer.DrawFullscreen(Color.Black);
				if (Starfield != null && !Starfield.IsDisposed)
				{
					Starfield.Draw();
				}
			}
			if (IntroPanDown != null && !IntroPanDown.DoPanDown && !didPanDown)
			{
				DoPanDown();
			}
			break;
		}
	}

	private void DoPanDown()
	{
		ServiceHelper.AddComponent(new TileTransition(ServiceHelper.Game)
		{
			ScreenCaptured = delegate
			{
				IntroPanDown.DoPanDown = true;
			},
			WaitFor = () => IntroPanDown.SinceStarted > 0f
		});
		didPanDown = true;
	}
}
