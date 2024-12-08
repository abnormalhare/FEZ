using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Common;
using EasyStorage;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
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
using Microsoft.Xna.Framework.Graphics.Localization;
using SDL2;
using Steamworks;

namespace FezGame.Components;

internal class MenuBase : DrawableGameComponent
{
	protected SpriteBatch SpriteBatch;

	protected MenuLevel CurrentMenuLevel;

	protected MenuLevel MenuRoot;

	protected MenuLevel UnlockNeedsLIVEMenu;

	protected MenuLevel HelpOptionsMenu;

	protected MenuLevel StartNewGameMenu;

	protected MenuLevel ExitToArcadeMenu;

	protected MenuLevel GameSettingsMenu;

	protected MenuLevel AudioSettingsMenu;

	protected MenuLevel VideoSettingsMenu;

	protected LeaderboardsMenuLevel LeaderboardsMenu;

	protected ControlsMenuLevel ControlsMenu;

	public CreditsMenuLevel CreditsMenu;

	protected List<MenuLevel> MenuLevels;

	protected MenuItem StereoMenuItem;

	protected MenuItem SinglethreadedMenuItem;

	protected MenuItem PauseOnLostFocusMenuItem;

	protected MenuItem ResolutionMenuItem;

	protected MenuItem DisableSteamworksMenuItem;

	protected SaveManagementLevel SaveManagementMenu;

	protected TimeSpan sliderDownLeft;

	public MenuLevel nextMenuLevel;

	protected MenuLevel lastMenuLevel;

	protected GlyphTextRenderer tr;

	protected Mesh Selector;

	protected Mesh Frame;

	protected Mesh Mask;

	protected SoundEffect sAdvanceLevel;

	protected SoundEffect sCancel;

	protected SoundEffect sConfirm;

	protected SoundEffect sCursorUp;

	protected SoundEffect sCursorDown;

	protected SoundEffect sExitGame;

	protected SoundEffect sReturnLevel;

	protected SoundEffect sScreenNarrowen;

	protected SoundEffect sScreenWiden;

	protected SoundEffect sSliderValueDecrease;

	protected SoundEffect sSliderValueIncrease;

	protected SoundEffect sStartGame;

	protected SoundEffect sAppear;

	protected SoundEffect sDisappear;

	protected SelectorPhase selectorPhase;

	protected float sinceSelectorPhaseStarted;

	protected RenderTarget2D CurrentMenuLevelTexture;

	protected RenderTarget2D NextMenuLevelTexture;

	protected Mesh MenuLevelOverlay;

	protected int currentResolution;

	protected ScreenMode currentScreenMode;

	protected ScaleMode currentScaleMode;

	protected bool vsync;

	protected bool hwInstancing;

	protected bool hiDpi;

	protected int msaa;

	protected bool lighting;

	protected Language languageToSet = Culture.Language;

	public bool EndGameMenu;

	protected bool StartedNewGame;

	public bool CursorSelectable;

	public bool CursorClicking;

	protected float SinceMouseMoved = 3f;

	private float sinceRestartNoteShown;

	protected Rectangle? AButtonRect;

	protected Rectangle? BButtonRect;

	protected Rectangle? XButtonRect;

	public static readonly Action SliderAction = delegate
	{
	};

	private Texture2D CanClickCursor;

	private Texture2D PointerCursor;

	private Texture2D ClickedCursor;

	protected bool isDisposed;

	[ServiceDependency]
	public IMouseStateManager MouseState { protected get; set; }

	[ServiceDependency]
	public IKeyboardStateManager KeyboardState { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	[ServiceDependency]
	public IInputManager InputManager { protected get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { protected get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { protected get; set; }

	[ServiceDependency]
	public IFontManager Fonts { protected get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { protected get; set; }

	protected MenuBase(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		KeyboardState.IgnoreMapping = true;
		CreditsMenu = new CreditsMenuLevel
		{
			Title = "Credits",
			Oversized = true,
			IsDynamic = true
		};
		StartNewGameMenu = new MenuLevel
		{
			Title = "StartNewGameTitle",
			AButtonStarts = true,
			AButtonString = "StartNewGameWithGlyph",
			AButtonAction = StartNewGame
		};
		StartNewGameMenu.AddItem("StartNewGameTextLine", Util.NullAction);
		ExitToArcadeMenu = new MenuLevel
		{
			Title = "ExitConfirmationTitle",
			AButtonString = "ExitChoiceYes",
			AButtonAction = ReturnToArcade
		};
		ExitToArcadeMenu.AddItem("ReturnToArcadeTextLine", Util.NullAction);
		LeaderboardsMenu = new LeaderboardsMenuLevel(this)
		{
			Title = "LeaderboardsTitle",
			Oversized = true
		};
		ControlsMenu = new ControlsMenuLevel(this)
		{
			Title = "Controls",
			Oversized = true
		};
		GameSettingsMenu = new MenuLevel
		{
			Title = "GameSettings",
			BButtonString = "MenuSaveWithGlyph",
			IsDynamic = true,
			Oversized = true
		};
		AudioSettingsMenu = new MenuLevel
		{
			Title = "AudioSettings",
			BButtonString = "MenuSaveWithGlyph",
			IsDynamic = true,
			Oversized = true
		};
		VideoSettingsMenu = new MenuLevel
		{
			Title = "VideoSettings",
			AButtonString = null,
			IsDynamic = true,
			Oversized = true
		};
		Action refreshVideoApplyString = delegate
		{
			bool flag = SettingsManager.Resolutions[currentResolution].Width != SettingsManager.Settings.Width || SettingsManager.Resolutions[currentResolution].Height != SettingsManager.Settings.Height || currentScreenMode != SettingsManager.Settings.ScreenMode || currentScaleMode != SettingsManager.Settings.ScaleMode || vsync != SettingsManager.Settings.VSync || hwInstancing != SettingsManager.Settings.HardwareInstancing || msaa != SettingsManager.Settings.MultiSampleCount || hiDpi != SettingsManager.Settings.HighDPI || lighting != SettingsManager.Settings.Lighting;
			VideoSettingsMenu.AButtonString = (flag ? "MenuApplyWithGlyph" : null);
		};
		ResolutionMenuItem = VideoSettingsMenu.AddItem("Resolution", ApplyVideo, defaultItem: false, delegate
		{
			if (SettingsManager.Settings.UseCurrentMode)
			{
				return SettingsManager.Settings.Width + "x" + SettingsManager.Settings.Height;
			}
			DisplayMode displayMode = SettingsManager.Resolutions[currentResolution];
			_ = displayMode.Width / 1280;
			_ = displayMode.Height / 720;
			return displayMode.Width + "x" + displayMode.Height;
		}, delegate(string lastValue, int change)
		{
			currentResolution += change;
			if (currentResolution == SettingsManager.Resolutions.Count)
			{
				currentResolution = 0;
			}
			if (currentResolution == -1)
			{
				currentResolution = SettingsManager.Resolutions.Count - 1;
			}
			refreshVideoApplyString();
		});
		ResolutionMenuItem.UpperCase = true;
		if (SettingsManager.Settings.UseCurrentMode)
		{
			ResolutionMenuItem.Disabled = true;
			ResolutionMenuItem.Selectable = false;
			VideoSettingsMenu.SelectedIndex++;
		}
		bool num = SDL.SDL_GetPlatform().Equals("Mac OS X");
		if (num)
		{
			VideoSettingsMenu.AddItem("HiDpiMode", ApplyVideo, defaultItem: false, () => (!hiDpi) ? StaticText.GetString("Off") : StaticText.GetString("On"), delegate
			{
				hiDpi = !hiDpi;
				refreshVideoApplyString();
			}).UpperCase = true;
		}
		VideoSettingsMenu.AddItem("ScreenMode", ApplyVideo, defaultItem: false, () => (currentScreenMode != 0) ? ((currentScreenMode != ScreenMode.Fullscreen) ? StaticText.GetString("Borderless") : StaticText.GetString("Fullscreen")) : StaticText.GetString("Windowed"), delegate(string _, int inc)
		{
			currentScreenMode += inc;
			if (currentScreenMode > ScreenMode.Fullscreen)
			{
				currentScreenMode = ScreenMode.Windowed;
			}
			if (currentScreenMode < ScreenMode.Windowed)
			{
				currentScreenMode = ScreenMode.Fullscreen;
			}
			refreshVideoApplyString();
		}).UpperCase = true;
		VideoSettingsMenu.AddItem("ScaleMode", ApplyVideo, defaultItem: false, () => (currentScaleMode != 0) ? ((currentScaleMode != ScaleMode.PixelPerfect) ? StaticText.GetString("Supersampling") : StaticText.GetString("PixelPerfect")) : StaticText.GetString("FullAspect"), delegate(string _, int inc)
		{
			currentScaleMode += inc;
			if (currentScaleMode > ScaleMode.Supersampled)
			{
				currentScaleMode = ScaleMode.FullAspect;
			}
			if (currentScaleMode < ScaleMode.FullAspect)
			{
				currentScaleMode = ScaleMode.Supersampled;
			}
			refreshVideoApplyString();
		}).UpperCase = true;
		VideoSettingsMenu.AddItem("Brightness", SliderAction, defaultItem: false, () => SettingsManager.Settings.Brightness, delegate(float lastValue, int change)
		{
			float num2 = ((lastValue <= 0.05f && change < 0) ? 0f : ((!(lastValue >= 0.95f) || change <= 0) ? (lastValue + (float)change * 0.05f) : 1f));
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			float brightness = (SettingsManager.Settings.Brightness = num2);
			graphicsDevice.SetGamma(brightness);
		}).UpperCase = true;
		bool flag3 = (VideoSettingsMenu.AddItem("VSync", ApplyVideo, defaultItem: false, () => (!vsync) ? StaticText.GetString("Off") : StaticText.GetString("On"), delegate
		{
			vsync = !vsync;
			refreshVideoApplyString();
		}).UpperCase = true);
		MenuItem<string> menuItem = VideoSettingsMenu.AddItem("HardwareInstancing", ApplyVideo, defaultItem: false, () => (!hwInstancing) ? StaticText.GetString("Off") : StaticText.GetString("On"), delegate
		{
			hwInstancing = !hwInstancing;
			refreshVideoApplyString();
		});
		menuItem.UpperCase = true;
		if (!SettingsManager.SupportsHardwareInstancing)
		{
			menuItem.Selectable = false;
			menuItem.Disabled = true;
		}
		if (SettingsManager.Settings.MultiSampleOption)
		{
			MenuItem<string> menuItem2 = VideoSettingsMenu.AddItem("MSAA", ApplyVideo, defaultItem: false, () => (msaa != 0) ? (msaa + "X") : StaticText.GetString("Off"), delegate(string lastValue, int change)
			{
				if (change > 0)
				{
					if (msaa == 0)
					{
						msaa = 2;
					}
					else
					{
						msaa *= 2;
					}
				}
				else if (msaa == 2)
				{
					msaa = 0;
				}
				else
				{
					msaa /= 2;
				}
				if (msaa > SettingsManager.MaxMultiSampleCount)
				{
					msaa = SettingsManager.MaxMultiSampleCount;
				}
				refreshVideoApplyString();
			});
			menuItem2.UpperCase = true;
			if (SettingsManager.MaxMultiSampleCount == 0)
			{
				menuItem2.Selectable = false;
				menuItem2.Disabled = true;
			}
		}
		flag3 = (VideoSettingsMenu.AddItem("Lighting", ApplyVideo, defaultItem: false, () => (!lighting) ? StaticText.GetString("Off") : StaticText.GetString("On"), delegate
		{
			lighting = !lighting;
			refreshVideoApplyString();
		}).UpperCase = true);
		VideoSettingsMenu.AddItem("ResetToDefault", ReturnToVideoDefault);
		if (num)
		{
			MenuLevel videoSettingsMenu = VideoSettingsMenu;
			videoSettingsMenu.OnPostDraw = (Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float>)Delegate.Combine(videoSettingsMenu.OnPostDraw, (Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float>)delegate(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha)
			{
				Viewport viewport = batch.GraphicsDevice.Viewport;
				Vector2 offset = new Vector2(-384f * batch.GraphicsDevice.GetViewScale(), (float)viewport.Height / 2f + 256f * batch.GraphicsDevice.GetViewScale());
				float scale = Fonts.SmallFactor * batch.GraphicsDevice.GetViewScale();
				if (VideoSettingsMenu.SelectedIndex == 1 && selectorPhase == SelectorPhase.Select)
				{
					sinceRestartNoteShown = Math.Min(sinceRestartNoteShown + 0.05f, 1f);
					tr.DrawCenteredString(batch, Fonts.Small, StaticText.GetString("RequiresRestart"), new Color(1f, 1f, 1f, alpha * sinceRestartNoteShown), offset, scale);
				}
				else
				{
					sinceRestartNoteShown = Math.Max(sinceRestartNoteShown - 0.1f, 0f);
				}
			});
		}
		AudioSettingsMenu.AddItem("SoundVolume", SliderAction, defaultItem: false, () => SettingsManager.Settings.SoundVolume, delegate(float lastValue, int change)
		{
			float num4 = ((lastValue <= 0.05f && change < 0) ? 0f : ((!(lastValue >= 0.95f) || change <= 0) ? (lastValue + (float)change * 0.05f) : 1f));
			ISoundManager soundManager = SoundManager;
			float soundEffectVolume = (SettingsManager.Settings.SoundVolume = num4);
			soundManager.SoundEffectVolume = soundEffectVolume;
		}).UpperCase = true;
		AudioSettingsMenu.AddItem("MusicVolume", SliderAction, defaultItem: false, () => SettingsManager.Settings.MusicVolume, delegate(float lastValue, int change)
		{
			float num6 = ((lastValue <= 0.05f && change < 0) ? 0f : ((!(lastValue >= 0.95f) || change <= 0) ? (lastValue + (float)change * 0.05f) : 1f));
			ISoundManager soundManager2 = SoundManager;
			float musicVolume = (SettingsManager.Settings.MusicVolume = num6);
			soundManager2.MusicVolume = musicVolume;
		}).UpperCase = true;
		AudioSettingsMenu.AddItem("ResetToDefault", ReturnToAudioDefault);
		MenuItem<Language> menuItem3 = GameSettingsMenu.AddItem("Language", SliderAction, defaultItem: false, () => languageToSet, delegate(Language lastValue, int change)
		{
			if (change < 0 && languageToSet == Language.English)
			{
				languageToSet = Language.Korean;
			}
			else if (change > 0 && languageToSet == Language.Korean)
			{
				languageToSet = Language.English;
			}
			else
			{
				languageToSet += change;
			}
		});
		GameSettingsMenu.AButtonString = null;
		menuItem3.Selected = delegate
		{
			Language language2 = (SettingsManager.Settings.Language = languageToSet);
			Culture.Language = language2;
		};
		GameSettingsMenu.OnReset = delegate
		{
			languageToSet = Culture.Language;
		};
		menuItem3.UpperCase = true;
		menuItem3.LocalizeSliderValue = true;
		menuItem3.LocalizationTagFormat = "Language{0}";
		bool hasStereo3D = HasStereo3D();
		if (hasStereo3D)
		{
			StereoMenuItem = GameSettingsMenu.AddItem(GameState.StereoMode ? "Stereo3DOn" : "Stereo3DOff", delegate
			{
			}, defaultItem: false, () => string.Empty, delegate
			{
				ToggleStereo();
			});
		}
		PauseOnLostFocusMenuItem = GameSettingsMenu.AddItem("PauseOnLostFocus", delegate
		{
		}, defaultItem: false, () => (!SettingsManager.Settings.PauseOnLostFocus) ? StaticText.GetString("Off") : StaticText.GetString("On"), delegate
		{
			TogglePauseOnLostFocus();
		});
		PauseOnLostFocusMenuItem.UpperCase = true;
		SinglethreadedMenuItem = GameSettingsMenu.AddItem("Singlethreaded", delegate
		{
		}, defaultItem: false, delegate
		{
			string text = (SettingsManager.Settings.Singlethreaded ? StaticText.GetString("On") : StaticText.GetString("Off"));
			if (PersistentThreadPool.SingleThreaded != SettingsManager.Settings.Singlethreaded)
			{
				text += " *";
			}
			return text;
		}, delegate
		{
			ToggleSinglethreaded();
		});
		SinglethreadedMenuItem.UpperCase = true;
		DisableSteamworksMenuItem = GameSettingsMenu.AddItem("Steamworks", delegate
		{
		}, defaultItem: false, delegate
		{
			string text2 = ((!SettingsManager.Settings.DisableSteamworks) ? StaticText.GetString("On") : StaticText.GetString("Off"));
			if (Fez.DisableSteamworksInit != SettingsManager.Settings.DisableSteamworks)
			{
				text2 += " *";
			}
			return text2;
		}, delegate
		{
			ToggleSteamworks();
		});
		DisableSteamworksMenuItem.UpperCase = true;
		GameSettingsMenu.AddItem("ResetToDefault", delegate
		{
			ReturnToGameDefault();
			languageToSet = Culture.Language;
		});
		MenuLevel gameSettingsMenu = GameSettingsMenu;
		gameSettingsMenu.OnPostDraw = (Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float>)Delegate.Combine(gameSettingsMenu.OnPostDraw, (Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float>)delegate(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha)
		{
			Viewport viewport2 = batch.GraphicsDevice.Viewport;
			Vector2 offset2 = new Vector2(-384f * batch.GraphicsDevice.GetViewScale(), (float)viewport2.Height / 2f + 256f * batch.GraphicsDevice.GetViewScale());
			float scale2 = Fonts.SmallFactor * batch.GraphicsDevice.GetViewScale();
			int num8 = 2;
			if (hasStereo3D)
			{
				num8++;
			}
			if (((GameSettingsMenu.SelectedIndex == num8) | (GameSettingsMenu.SelectedIndex == num8 + 1)) && selectorPhase == SelectorPhase.Select)
			{
				sinceRestartNoteShown = Math.Min(sinceRestartNoteShown + 0.05f, 1f);
				tr.DrawCenteredString(batch, Fonts.Small, StaticText.GetString("RequiresRestart"), new Color(1f, 1f, 1f, alpha * sinceRestartNoteShown), offset2, scale2);
			}
			else
			{
				sinceRestartNoteShown = Math.Max(sinceRestartNoteShown - 0.1f, 0f);
			}
		});
		SaveManagementMenu = new SaveManagementLevel(this);
		HelpOptionsMenu = new MenuLevel
		{
			Title = "HelpOptions"
		};
		HelpOptionsMenu.AddItem("Controls", delegate
		{
			ChangeMenuLevel(ControlsMenu);
		});
		HelpOptionsMenu.AddItem("GameSettings", delegate
		{
			ChangeMenuLevel(GameSettingsMenu);
		});
		HelpOptionsMenu.AddItem("VideoSettings", delegate
		{
			Settings s = SettingsManager.Settings;
			DisplayMode item = SettingsManager.Resolutions.FirstOrDefault((DisplayMode x) => x.Width == s.Width && x.Height == s.Height) ?? GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
			currentResolution = SettingsManager.Resolutions.IndexOf(item);
			if (currentResolution == -1 || currentResolution >= SettingsManager.Resolutions.Count)
			{
				currentResolution = 0;
			}
			currentScreenMode = SettingsManager.Settings.ScreenMode;
			currentScaleMode = SettingsManager.Settings.ScaleMode;
			vsync = SettingsManager.Settings.VSync;
			hwInstancing = SettingsManager.Settings.HardwareInstancing;
			hiDpi = SettingsManager.Settings.HighDPI;
			msaa = SettingsManager.Settings.MultiSampleCount;
			lighting = SettingsManager.Settings.Lighting;
			ChangeMenuLevel(VideoSettingsMenu);
		}).UpperCase = true;
		HelpOptionsMenu.AddItem("AudioSettings", delegate
		{
			ChangeMenuLevel(AudioSettingsMenu);
		});
		if (!Fez.PublicDemo)
		{
			HelpOptionsMenu.AddItem("SaveManagementTitle", delegate
			{
				ChangeMenuLevel(SaveManagementMenu);
			});
		}
		SaveManagementMenu.Parent = HelpOptionsMenu;
		GameSettingsMenu.Parent = HelpOptionsMenu;
		AudioSettingsMenu.Parent = HelpOptionsMenu;
		VideoSettingsMenu.Parent = HelpOptionsMenu;
		ControlsMenu.Parent = HelpOptionsMenu;
		UnlockNeedsLIVEMenu = new MenuLevel();
		UnlockNeedsLIVEMenu.AddItem("UnlockNeedsLIVE", SliderAction).Selectable = false;
		MenuRoot = new MenuLevel();
		MenuItem menuItem4 = MenuRoot.AddItem("HelpOptions", delegate
		{
			ChangeMenuLevel(HelpOptionsMenu);
		});
		if (Fez.PublicDemo)
		{
			menuItem4.Selectable = false;
			menuItem4.Disabled = true;
		}
		MenuItem menuItem5 = MenuRoot.AddItem("Leaderboards", delegate
		{
			ChangeMenuLevel(LeaderboardsMenu);
		});
		MenuItem menuItem6 = MenuRoot.AddItem("Achievements", ShowAchievements);
		MenuRoot.AddItem("Credits", delegate
		{
			ChangeMenuLevel(CreditsMenu);
		});
		CreditsMenu.Parent = MenuRoot;
		MenuItem menuItem7 = null;
		if (GameState.IsTrialMode)
		{
			menuItem7 = MenuRoot.AddItem("UnlockFullGame", UnlockFullGame);
		}
		MenuItem menuItem8 = MenuRoot.AddItem("ReturnToArcade", delegate
		{
			ChangeMenuLevel(ExitToArcadeMenu);
		});
		if (Fez.PublicDemo)
		{
			menuItem8.Disabled = true;
			flag3 = (menuItem6.Disabled = true);
			menuItem5.Disabled = flag3;
			if (menuItem7 != null)
			{
				menuItem7.Disabled = true;
			}
			menuItem8.Selectable = false;
			flag3 = (menuItem6.Selectable = false);
			menuItem5.Selectable = flag3;
			if (menuItem7 != null)
			{
				menuItem7.Selectable = false;
			}
		}
		if (Fez.NoSteamworks)
		{
			flag3 = (menuItem5.Disabled = true);
			menuItem6.Disabled = flag3;
			flag3 = (menuItem5.Selectable = false);
			menuItem6.Selectable = flag3;
		}
		else if (!SteamUtils.IsOverlayEnabled())
		{
			menuItem6.Disabled = true;
			menuItem6.Selectable = false;
		}
		MenuLevels = new List<MenuLevel>
		{
			MenuRoot, UnlockNeedsLIVEMenu, StartNewGameMenu, HelpOptionsMenu, AudioSettingsMenu, VideoSettingsMenu, GameSettingsMenu, ExitToArcadeMenu, LeaderboardsMenu, ControlsMenu,
			CreditsMenu, SaveManagementMenu
		};
		foreach (MenuLevel menuLevel in MenuLevels)
		{
			if (menuLevel != MenuRoot && menuLevel.Parent == null)
			{
				menuLevel.Parent = MenuRoot;
			}
		}
		nextMenuLevel = (EndGameMenu ? CreditsMenu : MenuRoot);
		GameState.DynamicUpgrade += DynamicUpgrade;
		PostInitialize();
		base.Initialize();
	}

	protected virtual void PostInitialize()
	{
	}

	protected override void LoadContent()
	{
		SpriteBatch = new SpriteBatch(base.GraphicsDevice);
		tr = new GlyphTextRenderer(base.Game);
		tr.IgnoreKeyboardRemapping = true;
		ContentManager contentManager = CMProvider.Get(CM.Menu);
		PointerCursor = contentManager.Load<Texture2D>("Other Textures/cursor/CURSOR_POINTER");
		CanClickCursor = contentManager.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_A");
		ClickedCursor = contentManager.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_B");
		sAdvanceLevel = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/AdvanceLevel");
		sCancel = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/Cancel");
		sConfirm = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/Confirm");
		sCursorUp = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/CursorUp");
		sCursorDown = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/CursorDown");
		sExitGame = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/Menu/ExitGame");
		sReturnLevel = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/ReturnLevel");
		sScreenNarrowen = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/ScreenNarrowen");
		sScreenWiden = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/ScreenWiden");
		sSliderValueDecrease = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/SliderValueDecrease");
		sSliderValueIncrease = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/SliderValueIncrease");
		sStartGame = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/Menu/StartGame");
		sAppear = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/Appear");
		sDisappear = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/Menu/Disappear");
		LeaderboardsMenu.InputManager = InputManager;
		LeaderboardsMenu.GameState = GameState;
		LeaderboardsMenu.Font = Fonts.Big;
		LeaderboardsMenu.MouseState = MouseState;
		ControlsMenu.FontManager = Fonts;
		ControlsMenu.CMProvider = CMProvider;
		CreditsMenu.FontManager = Fonts;
		foreach (MenuLevel menuLevel in MenuLevels)
		{
			menuLevel.CMProvider = CMProvider;
			menuLevel.Initialize();
		}
		Selector = new Mesh
		{
			Effect = new DefaultEffect.VertexColored
			{
				ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up),
				ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f)
			},
			DepthWrites = false,
			AlwaysOnTop = true,
			Culling = CullMode.None
		};
		Selector.AddLines(new Color[4]
		{
			Color.White,
			Color.White,
			Color.White,
			Color.White
		}, new Vector3(-1f, -1f, 10f), new Vector3(-1f, 1f, 10f), new Vector3(1f, 1f, 10f), new Vector3(1f, -1f, 10f));
		Selector.AddLines(new Color[4]
		{
			Color.White,
			Color.White,
			Color.White,
			Color.White
		}, new Vector3(-1f, 1f, 10f), new Vector3(0f, 1f, 10f), new Vector3(-1f, -1f, 10f), new Vector3(0f, -1f, 10f));
		Selector.AddLines(new Color[4]
		{
			Color.White,
			Color.White,
			Color.White,
			Color.White
		}, new Vector3(0f, 1f, 10f), new Vector3(1f, 1f, 10f), new Vector3(0f, -1f, 10f), new Vector3(1f, -1f, 10f));
		Frame = new Mesh
		{
			Effect = new DefaultEffect.VertexColored
			{
				ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up),
				ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f)
			},
			DepthWrites = false,
			AlwaysOnTop = true,
			Culling = CullMode.None,
			Enabled = false
		};
		Frame.AddLines(new Color[8]
		{
			Color.White,
			Color.White,
			Color.White,
			Color.White,
			Color.White,
			Color.White,
			Color.White,
			Color.White
		}, new Vector3(-1f, -1f, 10f), new Vector3(-1f, 1f, 10f), new Vector3(1f, 1f, 10f), new Vector3(1f, -1f, 10f), new Vector3(-1f, 1f, 10f), new Vector3(1f, 1f, 10f), new Vector3(-1f, -1f, 10f), new Vector3(1f, -1f, 10f));
		MenuLevelOverlay = new Mesh
		{
			Effect = new DefaultEffect.Textured
			{
				ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up),
				ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f)
			},
			DepthWrites = false,
			AlwaysOnTop = true,
			SamplerState = SamplerState.PointClamp
		};
		MenuLevelOverlay.AddFace(new Vector3(2f, 2f, 1f), new Vector3(0f, 0f, 10f), FaceOrientation.Back, centeredOnOrigin: true);
		Mask = new Mesh
		{
			Effect = new DefaultEffect.Textured
			{
				ForcedViewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up),
				ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f)
			},
			DepthWrites = false,
			AlwaysOnTop = true
		};
		Mask.AddFace(new Vector3(2f, 2f, 1f), new Vector3(0f, 0f, 10f), FaceOrientation.Back, centeredOnOrigin: true);
		Waiters.Wait(0.0, Rescale);
		RenderToTexture();
	}

	private void ToggleStereo()
	{
		GameState.StereoMode = !GameState.StereoMode;
		StereoMenuItem.Text = (GameState.StereoMode ? "Stereo3DOn" : "Stereo3DOff");
	}

	private void TogglePauseOnLostFocus()
	{
		SettingsManager.Settings.PauseOnLostFocus = !SettingsManager.Settings.PauseOnLostFocus;
	}

	private void ToggleSinglethreaded()
	{
		SettingsManager.Settings.Singlethreaded = !SettingsManager.Settings.Singlethreaded;
	}

	private void ToggleSteamworks()
	{
		SettingsManager.Settings.DisableSteamworks = !SettingsManager.Settings.DisableSteamworks;
	}

	protected virtual void ReturnToArcade()
	{
	}

	protected virtual void ContinueGame()
	{
	}

	protected virtual void ResumeGame()
	{
	}

	private void ReturnToVideoDefault()
	{
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		Settings settings = SettingsManager.Settings;
		float num2 = (SettingsManager.Settings.Brightness = 0.5f);
		float brightness = (settings.Brightness = num2);
		graphicsDevice.SetGamma(brightness);
		DisplayMode currentDisplayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
		currentResolution = SettingsManager.Resolutions.IndexOf(currentDisplayMode);
		if (currentResolution == -1 || currentResolution >= SettingsManager.Resolutions.Count)
		{
			currentResolution = 0;
		}
		currentScreenMode = ScreenMode.Fullscreen;
		currentScaleMode = ScaleMode.FullAspect;
		vsync = true;
		hwInstancing = SettingsManager.SupportsHardwareInstancing;
		hiDpi = false;
		msaa = 0;
		lighting = true;
		Settings settings2 = SettingsManager.Settings;
		settings2.UseCurrentMode = false;
		settings2.ScreenMode = ScreenMode.Fullscreen;
		settings2.Width = currentDisplayMode.Width;
		settings2.Height = currentDisplayMode.Height;
		settings2.ScreenMode = currentScreenMode;
		settings2.ScaleMode = currentScaleMode;
		settings2.VSync = vsync;
		settings2.HardwareInstancing = hwInstancing;
		settings2.MultiSampleCount = msaa;
		settings2.Lighting = lighting;
		settings2.HighDPI = hiDpi;
		SettingsManager.Apply();
		Rescale();
	}

	private void ApplyVideo()
	{
		DisplayMode displayMode = SettingsManager.Resolutions[currentResolution];
		SettingsManager.Settings.Width = displayMode.Width;
		SettingsManager.Settings.Height = displayMode.Height;
		SettingsManager.Settings.ScreenMode = currentScreenMode;
		SettingsManager.Settings.ScaleMode = currentScaleMode;
		SettingsManager.Settings.VSync = vsync;
		SettingsManager.Settings.HardwareInstancing = hwInstancing;
		SettingsManager.Settings.MultiSampleCount = msaa;
		SettingsManager.Settings.Lighting = lighting;
		SettingsManager.Settings.HighDPI = hiDpi;
		SettingsManager.Apply();
		VideoSettingsMenu.AButtonString = null;
		Rescale();
	}

	private void Rescale()
	{
		MenuLevelOverlay.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f);
		Mask.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f);
		Selector.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f);
		Frame.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height, 0.1f, 100f);
		int num = ((CurrentMenuLevel == null) ? 512 : (CurrentMenuLevel.Oversized ? 512 : 352));
		Frame.Scale = new Vector3(num, 256f, 1f) * base.GraphicsDevice.GetViewScale();
	}

	private void ReturnToAudioDefault()
	{
		ISoundManager soundManager = SoundManager;
		Settings settings = SettingsManager.Settings;
		ISoundManager soundManager2 = SoundManager;
		float num2 = (SettingsManager.Settings.MusicVolume = 1f);
		float num4 = (soundManager2.MusicVolume = num2);
		float soundEffectVolume = (settings.SoundVolume = num4);
		soundManager.SoundEffectVolume = soundEffectVolume;
	}

	private void ReturnToGameDefault()
	{
		if (StereoMenuItem != null)
		{
			GameState.StereoMode = true;
			ToggleStereo();
		}
		SettingsManager.Settings.PauseOnLostFocus = false;
		TogglePauseOnLostFocus();
		SettingsManager.Settings.Singlethreaded = true;
		ToggleSinglethreaded();
		SettingsManager.Settings.DisableSteamworks = true;
		ToggleSteamworks();
		SettingsManager.Settings.Language = (Culture.Language = Culture.LanguageFromCurrentCulture());
	}

	protected virtual void StartNewGame()
	{
		sinceSelectorPhaseStarted = 0f;
		selectorPhase = SelectorPhase.Disappear;
		sDisappear.Emit().Persistent = true;
	}

	private void ShowAchievements()
	{
		SteamFriends.ActivateGameOverlay("Achievements");
	}

	private void UnlockFullGame()
	{
	}

	private void DynamicUpgrade()
	{
		ServiceHelper.RemoveComponent(this);
		Console.WriteLine("Removed main menu component");
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		foreach (MenuLevel menuLevel in MenuLevels)
		{
			menuLevel.Dispose();
		}
		if (CurrentMenuLevelTexture != null)
		{
			CurrentMenuLevelTexture.Dispose();
			CurrentMenuLevelTexture = null;
		}
		if (NextMenuLevelTexture != null)
		{
			NextMenuLevelTexture.Dispose();
			NextMenuLevelTexture = null;
		}
		Selector.Dispose();
		Frame.Dispose();
		Mask.Dispose();
		MenuLevelOverlay.Dispose();
		GameState.UnPause();
		CMProvider.Dispose(CM.Menu);
		KeyboardState.IgnoreMapping = false;
		GameState.DynamicUpgrade -= DynamicUpgrade;
		isDisposed = true;
	}

	protected virtual bool UpdateEarlyOut()
	{
		return false;
	}

	protected virtual bool AllowDismiss()
	{
		return false;
	}

	public override void Update(GameTime gameTime)
	{
		UpdateSelector((float)gameTime.ElapsedGameTime.TotalSeconds);
		if (isDisposed || UpdateEarlyOut())
		{
			return;
		}
		MenuLevel menuLevel = nextMenuLevel ?? CurrentMenuLevel;
		if (menuLevel == null)
		{
			DestroyMenu();
			return;
		}
		if (menuLevel == GameSettingsMenu)
		{
			if (menuLevel.SelectedItem == menuLevel.Items.Last())
			{
				menuLevel.AButtonString = "MenuApplyWithGlyph";
			}
			else
			{
				menuLevel.AButtonString = ((menuLevel.SelectedIndex == 0 && SettingsManager.Settings.Language != languageToSet) ? "MenuApplyWithGlyph" : null);
			}
		}
		Point position = MouseState.Position;
		SinceMouseMoved += (float)gameTime.ElapsedGameTime.TotalSeconds;
		if (MouseState.Movement.X != 0 || MouseState.Movement.Y != 0)
		{
			SinceMouseMoved = 0f;
		}
		if (MouseState.LeftButton.State != 0)
		{
			SinceMouseMoved = 0f;
		}
		bool cursorSelectable = false;
		foreach (MenuItem item in menuLevel.Items)
		{
			if (item.Hidden || !item.Selectable)
			{
				continue;
			}
			if (item.HoverArea.Contains(position.X, position.Y))
			{
				cursorSelectable = item.Selected != new Action(Util.NullAction) && item.Selected != SliderAction;
				if (MouseState.Movement != Point.Zero)
				{
					int selectedIndex = menuLevel.SelectedIndex;
					menuLevel.SelectedIndex = menuLevel.Items.IndexOf(item);
					if (menuLevel.SelectedIndex > selectedIndex)
					{
						sCursorUp.Emit();
					}
					else if (menuLevel.SelectedIndex < selectedIndex)
					{
						sCursorDown.Emit();
					}
				}
				if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
				{
					Select(menuLevel);
				}
			}
			if (!item.IsSlider)
			{
				continue;
			}
			Rectangle hoverArea = item.HoverArea;
			hoverArea.X -= (int)((float)item.HoverArea.Height * 1.5f);
			hoverArea.Width = item.HoverArea.Height;
			Rectangle hoverArea2 = item.HoverArea;
			hoverArea2.X += item.HoverArea.Width + item.HoverArea.Height / 2;
			hoverArea2.Width = item.HoverArea.Height;
			if (hoverArea.Contains(position.X, position.Y))
			{
				cursorSelectable = true;
				if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
				{
					sSliderValueDecrease.Emit();
					CurrentMenuLevel.SelectedItem.Slide(-1);
				}
			}
			if (hoverArea2.Contains(position.X, position.Y))
			{
				cursorSelectable = true;
				if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
				{
					sSliderValueIncrease.Emit();
					CurrentMenuLevel.SelectedItem.Slide(1);
				}
			}
		}
		position = MouseState.PositionInViewport();
		if (AButtonRect.HasValue && AButtonRect.Value.Contains(position.X, position.Y))
		{
			cursorSelectable = true;
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				Select(menuLevel);
			}
		}
		if (BButtonRect.HasValue && BButtonRect.Value.Contains(position.X, position.Y))
		{
			cursorSelectable = true;
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				UpOneLevel(menuLevel);
			}
		}
		if (menuLevel.XButtonAction != null && XButtonRect.HasValue && XButtonRect.Value.Contains(position.X, position.Y))
		{
			cursorSelectable = true;
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				menuLevel.XButtonAction();
			}
		}
		CursorSelectable = cursorSelectable;
		CursorClicking = CursorSelectable && MouseState.LeftButton.State == MouseButtonStates.Down;
		if (!menuLevel.TrapInput)
		{
			if (InputManager.Up == FezButtonState.Pressed && menuLevel.MoveUp())
			{
				sCursorUp.Emit();
			}
			if (InputManager.Down == FezButtonState.Pressed && menuLevel.MoveDown())
			{
				sCursorDown.Emit();
			}
			if (((!EndGameMenu && InputManager.CancelTalk == FezButtonState.Pressed) || (EndGameMenu && InputManager.Start == FezButtonState.Pressed) || InputManager.Back == FezButtonState.Pressed || menuLevel.ForceCancel) && (AllowDismiss() || CurrentMenuLevel != MenuRoot))
			{
				UpOneLevel(menuLevel);
			}
			if (InputManager.Jump == FezButtonState.Pressed || InputManager.Start == FezButtonState.Pressed)
			{
				Select(menuLevel);
			}
			if (!Fez.PublicDemo && menuLevel.XButtonAction != null && InputManager.GrabThrow == FezButtonState.Pressed)
			{
				sConfirm.Emit();
				menuLevel.XButtonAction();
			}
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			if (CurrentMenuLevel != null && CurrentMenuLevel.SelectedItem != null && CurrentMenuLevel.SelectedItem.IsSlider)
			{
				if (InputManager.Left == FezButtonState.Down || InputManager.Right == FezButtonState.Down)
				{
					sliderDownLeft -= elapsedGameTime;
				}
				else
				{
					sliderDownLeft = TimeSpan.FromSeconds(0.30000001192092896);
				}
				if (InputManager.Left == FezButtonState.Pressed || (InputManager.Left == FezButtonState.Down && sliderDownLeft.Ticks <= 0))
				{
					if (sliderDownLeft.Ticks <= 0)
					{
						sliderDownLeft = TimeSpan.FromSeconds(0.10000000149011612);
					}
					sSliderValueDecrease.Emit();
					CurrentMenuLevel.SelectedItem.Slide(-1);
				}
				if (InputManager.Right == FezButtonState.Pressed || (InputManager.Right == FezButtonState.Down && sliderDownLeft.Ticks <= 0))
				{
					if (sliderDownLeft.Ticks <= 0)
					{
						sliderDownLeft = TimeSpan.FromSeconds(0.10000000149011612);
					}
					sSliderValueIncrease.Emit();
					CurrentMenuLevel.SelectedItem.Slide(1);
				}
			}
		}
		if (selectorPhase != 0)
		{
			menuLevel.Update(gameTime.ElapsedGameTime);
		}
	}

	private void UpOneLevel(MenuLevel activeLevel)
	{
		if (activeLevel != null && activeLevel.Items.Any((MenuItem x) => x.InError))
		{
			return;
		}
		sCancel.Emit();
		activeLevel.ForceCancel = false;
		if (EndGameMenu)
		{
			GameState.EndGame = true;
			GameState.Restart();
			base.Enabled = false;
			Waiters.Wait(0.4000000059604645, delegate
			{
				ServiceHelper.RemoveComponent(this);
			});
		}
		else if (activeLevel is SaveSlotSelectionLevel)
		{
			sinceSelectorPhaseStarted = 0f;
			selectorPhase = SelectorPhase.Disappear;
			GameState.ReturnToArcade();
		}
		else
		{
			if (activeLevel.Parent == HelpOptionsMenu)
			{
				SettingsManager.Save();
			}
			ChangeMenuLevel(activeLevel.Parent);
		}
	}

	private void Select(MenuLevel activeLevel)
	{
		if (activeLevel.AButtonAction == new Action(StartNewGame) || (activeLevel.SelectedItem != null && (activeLevel.SelectedItem.Selected == new Action(ContinueGame) || activeLevel.SelectedItem.Selected == new Action(StartNewGame))))
		{
			sStartGame.Emit().Persistent = true;
		}
		else if (activeLevel.AButtonAction == new Action(ReturnToArcade) && !GameState.IsTrialMode)
		{
			SoundManager.KillSounds();
			sExitGame.Emit().Persistent = true;
		}
		else if ((activeLevel.AButtonAction != null || activeLevel.SelectedItem != null) && activeLevel.SelectedItem.Selected != SliderAction)
		{
			sConfirm.Emit();
		}
		if (activeLevel.AButtonAction != null)
		{
			activeLevel.AButtonAction();
		}
		else
		{
			activeLevel.Select();
		}
	}

	private void UpdateSelector(float elapsedSeconds)
	{
		Vector3 vector = Vector3.Zero;
		Vector3 value = Vector3.Zero;
		float viewScale = base.GraphicsDevice.GetViewScale();
		if (CurrentMenuLevel != null && CurrentMenuLevel.SelectedItem != null)
		{
			float num = (float)(CurrentMenuLevel.Oversized ? 512 : 256) * viewScale;
			int num2 = CurrentMenuLevel.Items.Count((MenuItem x) => !x.Hidden);
			float num3 = ((CurrentMenuLevel.Items.Count == 0) ? 0f : ((CurrentMenuLevel.SelectedItem.Size.Y + Fonts.TopSpacing) * Fonts.BigFactor));
			int num4 = CurrentMenuLevel.SelectedIndex;
			MenuItem menuItem = CurrentMenuLevel.Items[num4];
			vector = new Vector3((menuItem.Size + new Vector2(Fonts.SideSpacing * 2f, Fonts.TopSpacing)) * Fonts.BigFactor / 2f, 1f);
			if (num2 > 10)
			{
				bool flag = false;
				switch (Culture.Language)
				{
				default:
					flag = true;
					break;
				case Language.English:
				case Language.Chinese:
				case Language.Japanese:
				case Language.Korean:
					break;
				}
				for (int i = 0; i <= CurrentMenuLevel.SelectedIndex; i++)
				{
					if (CurrentMenuLevel.Items[i].Hidden)
					{
						num4--;
					}
				}
				float num5 = 5f;
				if (num4 == num2 - 1)
				{
					value = new Vector3(0f, (num5 - 9f) * num3 - num3 / 2f, 0f);
				}
				else if (num4 < 8)
				{
					value = new Vector3(num / 2f, (num5 - (float)num4) * num3 - num3 / 2f, 0f);
				}
				else
				{
					num4 -= 8;
					value = new Vector3((0f - num) / 2f, (num5 - (float)num4) * num3 - num3 / 2f, 0f);
				}
				if (flag && num4 != num2 - 1)
				{
					vector = vector * Fonts.SmallFactor / Fonts.BigFactor;
				}
				string obj = WordWrap.Split(menuItem.ToString(), maxTextSize: ((float)base.Game.GraphicsDevice.Viewport.Width * 0.45f + value.X / 2f) / (Fonts.SmallFactor * viewScale), font: Fonts.Small);
				int num6 = 0;
				string text2 = obj;
				for (int j = 0; j < text2.Length; j++)
				{
					if (text2[j] == '\n')
					{
						num6++;
					}
				}
				if (num6 > 0)
				{
					vector.Y *= 1 + num6;
				}
			}
			else
			{
				float num7 = (float)num2 / 2f;
				for (int k = 0; k <= CurrentMenuLevel.SelectedIndex; k++)
				{
					if (CurrentMenuLevel.Items[k].Hidden)
					{
						num4--;
					}
				}
				value = new Vector3(0f, (num7 - (float)num4) * num3 - num3 / 2f, 0f);
			}
		}
		sinceSelectorPhaseStarted += elapsedSeconds;
		switch (selectorPhase)
		{
		case SelectorPhase.Appear:
		case SelectorPhase.Disappear:
		{
			Group group = Selector.Groups[0];
			Group group2 = Selector.Groups[1];
			Group group3 = Selector.Groups[2];
			Frame.Enabled = false;
			Selector.Material.Opacity = 1f;
			Selector.Enabled = true;
			Selector.Position = Vector3.Zero;
			Selector.Scale = Vector3.One;
			float num11 = Easing.EaseInOut(FezMath.Saturate(sinceSelectorPhaseStarted / 0.75f), EasingType.Sine, EasingType.Cubic);
			if (selectorPhase == SelectorPhase.Disappear)
			{
				num11 = 1f - num11;
			}
			bool enabled = (group3.Enabled = num11 > 0.5f);
			group2.Enabled = enabled;
			float num12 = (float)(nextMenuLevel.Oversized ? 512 : 352) * viewScale;
			float num13 = FezMath.Saturate((num11 - 0.5f) * 2f);
			float num14 = FezMath.Saturate(num11 * 2f);
			group.Scale = new Vector3(num12, 256f * num14 * viewScale, 1f);
			group2.Scale = new Vector3(num12 * num13, 256f * viewScale, 1f);
			group2.Position = new Vector3((0f - num12) * (1f - num13), 0f, 1f);
			group3.Scale = new Vector3(num12 * num13, 256f * viewScale, 1f);
			group3.Position = new Vector3(num12 * (1f - num13), 0f, 1f);
			if (num11 <= 0f && selectorPhase == SelectorPhase.Disappear && !StartedNewGame)
			{
				DestroyMenu();
			}
			if (num11 >= 1f && selectorPhase == SelectorPhase.Appear)
			{
				selectorPhase = SelectorPhase.Shrink;
				Vector3 vector2 = (group3.Scale = Vector3.One);
				Vector3 scale = (group2.Scale = vector2);
				group.Scale = scale;
				scale = (group3.Position = Vector3.Zero);
				group2.Position = scale;
				Mesh frame = Frame;
				scale = (Selector.Scale = new Vector3(num12, 256f * viewScale, 1f));
				frame.Scale = scale;
				Frame.Enabled = true;
				sinceSelectorPhaseStarted = 0f;
				CurrentMenuLevel = nextMenuLevel;
				CurrentMenuLevelTexture = NextMenuLevelTexture;
			}
			break;
		}
		case SelectorPhase.Shrink:
		{
			float num15 = Easing.EaseInOut(FezMath.Saturate(sinceSelectorPhaseStarted * 2.5f), EasingType.Sine, EasingType.Cubic);
			if (CurrentMenuLevel.SelectedItem == null || !CurrentMenuLevel.SelectedItem.Selectable)
			{
				Selector.Material.Opacity = 0f;
			}
			else
			{
				Selector.Material.Opacity = 1f;
				Selector.Scale = Vector3.Lerp(new Vector3((lastMenuLevel ?? CurrentMenuLevel).Oversized ? 512 : 352, 256f, 1f) * viewScale, vector, num15);
				Selector.Position = Vector3.Lerp(Vector3.Zero, value, num15);
			}
			Frame.Scale = Vector3.Lerp(new Vector3((lastMenuLevel ?? CurrentMenuLevel).Oversized ? 512 : 352, 256f, 1f) * viewScale, new Vector3(CurrentMenuLevel.Oversized ? 512 : 352, 256f, 1f) * viewScale, num15);
			if (num15 >= 1f)
			{
				selectorPhase = SelectorPhase.Select;
			}
			break;
		}
		case SelectorPhase.Select:
			if (CurrentMenuLevel.SelectedItem == null || !CurrentMenuLevel.SelectedItem.Selectable)
			{
				Selector.Material.Opacity = 0f;
				break;
			}
			Selector.Material.Opacity = 1f;
			Selector.Scale = Vector3.Lerp(Selector.Scale, vector, 0.3f);
			Selector.Position = Vector3.Lerp(Selector.Position, value, 0.3f);
			break;
		case SelectorPhase.FadeIn:
		{
			float num9 = Easing.EaseInOut(FezMath.Saturate(sinceSelectorPhaseStarted / 0.25f), EasingType.Sine, EasingType.Cubic);
			Selector.Material.Opacity = num9;
			Selector.Scale = Vector3.Lerp(Selector.Scale, vector, 0.3f);
			Selector.Position = Vector3.Lerp(Selector.Position, value, 0.3f);
			float num10 = (float)(CurrentMenuLevel.Oversized ? 512 : 352) * viewScale;
			if (Frame.Scale.X != num10)
			{
				Frame.Scale = Vector3.Lerp(new Vector3((lastMenuLevel ?? CurrentMenuLevel).Oversized ? 512 : 352, 256f, 1f) * viewScale, new Vector3(num10, 256f * viewScale, 1f), num9);
			}
			if (num9 >= 1f)
			{
				selectorPhase = SelectorPhase.Select;
				sinceSelectorPhaseStarted = 0f;
			}
			break;
		}
		case SelectorPhase.Grow:
		{
			float num8 = 1f - Easing.EaseInOut(FezMath.Saturate(sinceSelectorPhaseStarted / 0.3f), EasingType.Sine, EasingType.Quadratic);
			if (CurrentMenuLevel.SelectedItem == null || !CurrentMenuLevel.SelectedItem.Selectable)
			{
				Selector.Material.Opacity = 0f;
			}
			else
			{
				Selector.Material.Opacity = 1f;
				Selector.Scale = Vector3.Lerp(new Vector3(nextMenuLevel.Oversized ? 512 : 352, 256f, 1f) * viewScale, vector, num8);
				Selector.Position = Vector3.Lerp(Vector3.Zero, value, num8);
			}
			Frame.Scale = Vector3.Lerp(new Vector3(CurrentMenuLevel.Oversized ? 512 : 352, 256f, 1f) * viewScale, new Vector3(nextMenuLevel.Oversized ? 512 : 352, 256f, 1f) * viewScale, 1f - num8);
			if (num8 <= 0f)
			{
				lastMenuLevel = CurrentMenuLevel;
				CurrentMenuLevel = nextMenuLevel;
				CurrentMenuLevelTexture = NextMenuLevelTexture;
				if (CurrentMenuLevel.SelectedItem == null || !CurrentMenuLevel.SelectedItem.Selectable)
				{
					CurrentMenuLevel.Reset();
					selectorPhase = SelectorPhase.Select;
				}
				else
				{
					CurrentMenuLevel.Reset();
					selectorPhase = SelectorPhase.FadeIn;
				}
				sinceSelectorPhaseStarted = 0f;
			}
			break;
		}
		}
	}

	private void DestroyMenu()
	{
		ServiceHelper.RemoveComponent(this);
		nextMenuLevel = (CurrentMenuLevel = null);
	}

	public bool ChangeMenuLevel(MenuLevel next, bool silent = false)
	{
		if (CurrentMenuLevel == null)
		{
			return false;
		}
		bool flag = CurrentMenuLevel.SelectedItem == null || !CurrentMenuLevel.SelectedItem.Selectable;
		selectorPhase = (flag ? SelectorPhase.FadeIn : SelectorPhase.Grow);
		bool flag2 = next == CurrentMenuLevel.Parent;
		if (CurrentMenuLevel.OnClose != null)
		{
			CurrentMenuLevel.OnClose();
		}
		if (next == null)
		{
			ResumeGame();
			return true;
		}
		nextMenuLevel = next;
		nextMenuLevel.Reset();
		RenderToTexture();
		sinceSelectorPhaseStarted = 0f;
		lastMenuLevel = CurrentMenuLevel;
		if (flag)
		{
			CurrentMenuLevel = nextMenuLevel;
			CurrentMenuLevelTexture = NextMenuLevelTexture;
			if (CurrentMenuLevel == null)
			{
				DestroyMenu();
			}
		}
		else if (!silent)
		{
			if (flag2)
			{
				sReturnLevel.Emit();
			}
			else
			{
				sAdvanceLevel.Emit();
			}
			if (lastMenuLevel.Oversized && !CurrentMenuLevel.Oversized)
			{
				sScreenNarrowen.Emit();
			}
		}
		if (!lastMenuLevel.Oversized && CurrentMenuLevel.Oversized)
		{
			sScreenWiden.Emit();
		}
		return true;
	}

	private void RenderToTexture()
	{
		float viewScale = base.GraphicsDevice.GetViewScale();
		if (CurrentMenuLevel != null)
		{
			if (CurrentMenuLevelTexture != null)
			{
				CurrentMenuLevelTexture.Tag = "DISPOSED";
				CurrentMenuLevelTexture.Dispose();
			}
			CurrentMenuLevelTexture = new RenderTarget2D(base.GraphicsDevice, FezMath.Round((float)(2 * (CurrentMenuLevel.Oversized ? 512 : 352)) * viewScale), (int)(512f * viewScale), mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PlatformContents);
			CurrentMenuLevelTexture.Tag = "Current | " + CurrentMenuLevel.Title;
			base.GraphicsDevice.SetRenderTarget(CurrentMenuLevelTexture);
			base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
			SpriteBatch.BeginPoint();
			DrawLevel(CurrentMenuLevel, toTexture: true);
			SpriteBatch.End();
			base.GraphicsDevice.SetRenderTarget(null);
		}
		if (nextMenuLevel != null)
		{
			if (NextMenuLevelTexture != null)
			{
				NextMenuLevelTexture.Tag = "DISPOSED";
				NextMenuLevelTexture.Dispose();
			}
			NextMenuLevelTexture = new RenderTarget2D(base.GraphicsDevice, FezMath.Round((float)(2 * (nextMenuLevel.Oversized ? 512 : 352)) * viewScale), (int)(512f * viewScale), mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PlatformContents);
			NextMenuLevelTexture.Tag = "Next | " + nextMenuLevel.Title;
			base.GraphicsDevice.SetRenderTarget(NextMenuLevelTexture);
			base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
			SpriteBatch.BeginPoint();
			DrawLevel(nextMenuLevel, toTexture: true);
			SpriteBatch.End();
			base.GraphicsDevice.SetRenderTarget(null);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		float viewScale = base.GraphicsDevice.GetViewScale();
		Viewport viewport = base.GraphicsDevice.Viewport;
		int num = ((!Culture.IsCJK) ? 1 : (-1));
		float num2 = (Culture.IsCJK ? (Fonts.BigFactor + 0.25f) : (Fonts.BigFactor + 1f));
		num2 *= viewScale;
		Vector2 vector = new Vector2((float)viewport.Width / 2f, (float)viewport.Height / 2f - 256f * viewScale - num2 * (25f + Fonts.TopSpacing * 9f) + Fonts.TopSpacing * num2 * (float)num);
		Mask.Position = Selector.Position;
		Mask.Scale = Selector.Scale;
		bool isCJK = Culture.IsCJK;
		if (selectorPhase != SelectorPhase.Select && selectorPhase != SelectorPhase.Disappear)
		{
			graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
			graphicsDevice.PrepareStencilWrite(StencilMask.MenuWipe);
			Mask.Draw();
			graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		}
		if (selectorPhase == SelectorPhase.Grow)
		{
			MenuLevelOverlay.Scale = new Vector3((float)(nextMenuLevel.Oversized ? 512 : 352) * viewScale, 256f * viewScale, 1f);
			graphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.MenuWipe);
			MenuLevelOverlay.Texture = NextMenuLevelTexture;
			MenuLevelOverlay.Draw();
			graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			if (!isCJK)
			{
				SpriteBatch.BeginPoint();
			}
			else
			{
				SpriteBatch.BeginLinear();
			}
			if (nextMenuLevel.Title != null)
			{
				tr.DrawCenteredString(SpriteBatch, Fonts.Big, nextMenuLevel.Title, new Color(1f, 1f, 1f, sinceSelectorPhaseStarted / 0.3f), new Vector2(0f, vector.Y), num2);
			}
			SpriteBatch.End();
		}
		else
		{
			graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		}
		if (selectorPhase == SelectorPhase.Shrink)
		{
			MenuLevelOverlay.Scale = new Vector3((float)(CurrentMenuLevel.Oversized ? 512 : 352) * viewScale, 256f * viewScale, 1f);
			MenuLevelOverlay.Texture = CurrentMenuLevelTexture;
			MenuLevelOverlay.Draw();
			if (!isCJK)
			{
				SpriteBatch.BeginPoint();
			}
			else
			{
				SpriteBatch.BeginLinear();
			}
			if (nextMenuLevel.Title != null)
			{
				tr.DrawCenteredString(SpriteBatch, Fonts.Big, nextMenuLevel.Title, Color.White, new Vector2(0f, vector.Y), num2);
			}
			SpriteBatch.End();
		}
		if ((selectorPhase == SelectorPhase.Select || selectorPhase == SelectorPhase.FadeIn) && CurrentMenuLevel != null)
		{
			if (!CurrentMenuLevel.IsDynamic)
			{
				MenuLevelOverlay.Scale = new Vector3((float)(CurrentMenuLevel.Oversized ? 512 : 352) * viewScale, 256f * viewScale, 1f);
				MenuLevelOverlay.Texture = CurrentMenuLevelTexture;
				MenuLevelOverlay.Draw();
			}
			if (!isCJK)
			{
				SpriteBatch.BeginPoint();
			}
			else
			{
				SpriteBatch.BeginLinear();
			}
			if (CurrentMenuLevel.IsDynamic)
			{
				DrawLevel(CurrentMenuLevel, toTexture: false);
			}
			if (CurrentMenuLevel.Title != null)
			{
				tr.DrawCenteredString(SpriteBatch, Fonts.Big, CurrentMenuLevel.Title, Color.White, new Vector2(0f, vector.Y), num2);
			}
			SpriteBatch.End();
		}
		Selector.Draw();
		Frame.Draw();
		if (CurrentMenuLevel != null && selectorPhase != SelectorPhase.Disappear)
		{
			DrawButtons();
		}
		SpriteBatch.BeginPoint();
		float num3 = viewScale * 2f;
		Point point = MouseState.PositionInViewport();
		SpriteBatch.Draw(CursorClicking ? ClickedCursor : (CursorSelectable ? CanClickCursor : PointerCursor), new Vector2((float)point.X - num3 * 11.5f, (float)point.Y - num3 * 8.5f), null, new Color(1f, 1f, 1f, FezMath.Saturate(1f - (SinceMouseMoved - 2f))), 0f, Vector2.Zero, num3, SpriteEffects.None, 0f);
		SpriteBatch.End();
	}

	protected virtual bool AlwaysShowBackButton()
	{
		return false;
	}

	private void DrawButtons()
	{
		Viewport viewport = base.GraphicsDevice.Viewport;
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = Frame.Scale.X;
		if (512f * viewScale > (float)viewport.Width / 2f)
		{
			num = 352f * viewScale;
		}
		Vector2 position = new Vector2((float)viewport.Width / 2f + num - 5f, (float)viewport.Height / 2f + 512f * viewScale / 2f + 5f + Fonts.TopSpacing * Fonts.BigFactor);
		MenuLevel menuLevel = ((selectorPhase == SelectorPhase.Grow) ? nextMenuLevel : CurrentMenuLevel);
		bool flag = (AlwaysShowBackButton() || menuLevel != MenuRoot) && (!EndGameMenu || menuLevel != CreditsMenu);
		if (menuLevel is SaveSlotSelectionLevel)
		{
			flag = true;
		}
		bool flag2 = menuLevel.XButtonString != null;
		bool flag3 = menuLevel.AButtonString != null;
		if (menuLevel == VideoSettingsMenu && menuLevel.SelectedIndex == menuLevel.Items.Count - 1)
		{
			flag3 = true;
		}
		if (flag3 && flag && flag2)
		{
			switch (Culture.TwoLetterISOLanguageName)
			{
			case "en":
				position.X += 60f;
				break;
			case "fr":
				position.X += 230f;
				break;
			case "de":
				position.X += 210f;
				break;
			case "es":
				position.X += 230f;
				break;
			case "it":
				position.X += 125f;
				break;
			case "pt":
				position.X += 185f;
				break;
			}
		}
		if (menuLevel == LeaderboardsMenu)
		{
			position.X += 45f;
		}
		SpriteBatch.BeginPoint();
		SpriteFont small = Fonts.Small;
		float num2 = Fonts.SmallFactor * viewScale;
		if (flag)
		{
			string text = menuLevel.BButtonString ?? StaticText.GetString("MenuBackWithGlyph");
			if (!GamepadState.AnyConnected)
			{
				text = text.Replace("{B}", "{BACK}");
			}
			Vector2 vector = small.MeasureString(tr.FillInGlyphs(text.ToUpper(CultureInfo.InvariantCulture))) * num2;
			position -= vector * Vector2.UnitX;
			tr.DrawShadowedText(SpriteBatch, small, text.ToUpper(CultureInfo.InvariantCulture), position, new Color(1f, 0.5f, 0.5f, 1f), num2);
			BButtonRect = new Rectangle((int)position.X, (int)position.Y, (int)vector.X, (int)vector.Y);
			position -= tr.Margin * Vector2.UnitX / 4f;
		}
		else
		{
			BButtonRect = null;
		}
		if (flag2)
		{
			Vector2 vector2 = small.MeasureString(tr.FillInGlyphs(menuLevel.XButtonString.ToUpper(CultureInfo.InvariantCulture))) * num2;
			position -= vector2 * Vector2.UnitX;
			tr.DrawShadowedText(SpriteBatch, small, menuLevel.XButtonString.ToUpper(CultureInfo.InvariantCulture), position, new Color(0.5f, 0.5f, 1f, 1f), num2);
			XButtonRect = new Rectangle((int)position.X, (int)position.Y, (int)vector2.X, (int)vector2.Y);
			position -= tr.Margin * Vector2.UnitX / 4f;
		}
		else
		{
			XButtonRect = null;
		}
		if (flag3)
		{
			string text2 = menuLevel.AButtonString ?? StaticText.GetString("MenuApplyWithGlyph");
			if (!GamepadState.AnyConnected)
			{
				text2 = text2.Replace("{A}", "{START}");
			}
			Vector2 vector3 = small.MeasureString(tr.FillInGlyphs(text2.ToUpper(CultureInfo.InvariantCulture))) * num2;
			position -= vector3 * Vector2.UnitX;
			tr.DrawShadowedText(SpriteBatch, small, text2.ToUpper(CultureInfo.InvariantCulture), position, new Color(0.5f, 1f, 0.5f, 1f), num2);
			AButtonRect = new Rectangle((int)position.X, (int)position.Y, (int)vector3.X, (int)vector3.Y);
			position -= tr.Margin * Vector2.UnitX / 4f;
		}
		else
		{
			AButtonRect = null;
		}
		SpriteBatch.End();
	}

	private void DrawLevel(MenuLevel level, bool toTexture)
	{
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = (toTexture ? (512f * viewScale) : ((float)base.GraphicsDevice.Viewport.Height));
		bool flag = false;
		switch (Culture.Language)
		{
		default:
			flag = true;
			break;
		case Language.English:
		case Language.Chinese:
		case Language.Japanese:
		case Language.Korean:
			break;
		}
		lock (level)
		{
			SpriteFont spriteFont = ((Culture.IsCJK && viewScale > 1.5f) ? Fonts.Big : Fonts.Small);
			int num2 = 0;
			for (int i = 0; i < level.Items.Count; i++)
			{
				if (!level.Items[i].Hidden)
				{
					num2++;
				}
			}
			float num3 = (float)(level.Oversized ? 512 : 256) * viewScale;
			Point point = default(Point);
			for (int j = 0; j < level.Items.Count; j++)
			{
				MenuItem menuItem = level.Items[j];
				if (menuItem.Hidden)
				{
					continue;
				}
				bool flag2 = false;
				string text = menuItem.ToString();
				Vector2 size = tr.MeasureWithGlyphs(Fonts.Big, text, viewScale);
				if (string.IsNullOrEmpty(menuItem.Text))
				{
					size = Fonts.Big.MeasureString("A");
				}
				menuItem.Size = size;
				float num4 = ((level.Items.Count == 0) ? 0f : ((menuItem.Size.Y + Fonts.TopSpacing) * Fonts.BigFactor));
				float num5 = Fonts.BigFactor * viewScale;
				if (Culture.IsCJK && viewScale <= 1.5f)
				{
					num5 *= 2f;
				}
				int num6 = j;
				Vector3 vector;
				if (num2 > 10)
				{
					for (int k = 0; k <= j; k++)
					{
						if (level.Items[k].Hidden)
						{
							num6--;
						}
					}
					flag2 = num2 > 10 && num6 != num2 - 1;
					if (flag)
					{
						num5 = ((menuItem.IsGamerCard || flag2) ? Fonts.SmallFactor : Fonts.BigFactor) * viewScale;
					}
					float num7 = 5f;
					if (num6 == num2 - 1)
					{
						vector = new Vector3(0f, (num7 - 9f) * num4 - num4 / 2f, 0f);
					}
					else if (num6 < 8)
					{
						vector = new Vector3((0f - num3) / 2f, (num7 - (float)num6) * num4 - num4 / 2f, 0f);
					}
					else
					{
						num6 -= 8;
						vector = new Vector3(num3 / 2f, (num7 - (float)num6) * num4 - num4 / 2f, 0f);
					}
					if (flag2)
					{
						float num8 = (float)base.Game.GraphicsDevice.Viewport.Width * 0.45f;
						text = WordWrap.Split(text, spriteFont, (num8 - vector.X / 2f) / num5);
						int num9 = 0;
						string text2 = text;
						for (int l = 0; l < text2.Length; l++)
						{
							if (text2[l] == '\n')
							{
								num9++;
							}
						}
						if (num9 > 0)
						{
							size = tr.MeasureWithGlyphs(Fonts.Small, text, viewScale);
							num4 = (size.Y + Fonts.TopSpacing) * Fonts.SmallFactor;
							menuItem.Size = new Vector2(size.X, menuItem.Size.Y);
						}
						else if (flag)
						{
							num4 = ((level.Items.Count == 0) ? 0f : ((menuItem.Size.Y + Fonts.TopSpacing) * Fonts.SmallFactor));
							size.X *= Fonts.SmallFactor / Fonts.BigFactor;
						}
					}
				}
				else
				{
					float num10 = (float)num2 / 2f;
					for (int m = 0; m <= j; m++)
					{
						if (level.Items[m].Hidden)
						{
							num6--;
						}
					}
					vector = new Vector3(0f, (num10 - (float)num6) * num4 - num4 / 2f, 0f);
				}
				vector.Y *= -1f;
				vector.Y += num / 2f;
				vector.Y -= num4 / 2f;
				if (Culture.IsCJK)
				{
					vector.Y += viewScale * 4f;
				}
				SpriteFont font = (((menuItem.IsGamerCard || flag2) && !Culture.IsCJK) ? Fonts.Small : spriteFont);
				Color color = (menuItem.Disabled ? new Color(0.2f, 0.2f, 0.2f, 1f) : (menuItem.InError ? new Color(1f, 0f, 0f, 1f) : new Color(1f, 1f, 1f, 1f)));
				if (menuItem.IsGamerCard)
				{
					color = new Color(0.5f, 1f, 0.5f, 1f);
				}
				tr.DrawCenteredString(SpriteBatch, font, text, color, vector.XY(), num5);
				Vector2 vector2 = tr.MeasureWithGlyphs(font, text, num5);
				point.X = (int)((float)base.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f + vector.X - vector2.X / 2f);
				point.Y = (int)(vector.Y + (float)base.GraphicsDevice.PresentationParameters.BackBufferHeight / 2f - num / 2f);
				menuItem.HoverArea = new Rectangle(point.X, point.Y, (int)vector2.X, (int)vector2.Y);
				if (menuItem.IsSlider && level.SelectedItem == menuItem)
				{
					vector.Y += 7f * num5;
					if (flag2 && flag && num5 / viewScale < 2f)
					{
						vector.Y -= 4f * viewScale;
					}
					float num11 = viewScale * 20f * num5 / (Fonts.BigFactor * viewScale);
					if (Culture.IsCJK)
					{
						num11 *= 0.475f * viewScale;
						vector.Y += viewScale * 5f;
					}
					tr.DrawCenteredString(SpriteBatch, Fonts.Big, "{LA}", new Color(1f, 1f, 1f, 1f), new Vector2(vector.X - size.X / 2f * Fonts.BigFactor - num11 * 2f, vector.Y), (Culture.IsCJK ? 0.2f : 1f) * viewScale);
					tr.DrawCenteredString(SpriteBatch, Fonts.Big, "{RA}", new Color(1f, 1f, 1f, 1f), new Vector2(vector.X + size.X / 2f * Fonts.BigFactor + num11 * 2f, vector.Y), (Culture.IsCJK ? 0.2f : 1f) * viewScale);
				}
			}
			Viewport viewport = base.GraphicsDevice.Viewport;
			int num12 = FezMath.Round((float)(2 * (level.Oversized ? 512 : 352)) * viewScale);
			int num13 = (int)(512f * viewScale);
			if (viewport.Width == num12 && viewport.Height == num13)
			{
				level.PostDraw(SpriteBatch, spriteFont, tr, 1f);
				return;
			}
			SpriteBatch.End();
			base.GraphicsDevice.Viewport = new Viewport(0, 0, num12, num13);
			SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, base.GraphicsDevice.SamplerStates[0], null, RasterizerState.CullCounterClockwise, null, Matrix.CreateTranslation(new Vector3((float)(viewport.Width - num12) / 2f, (float)(viewport.Height - num13) / 2f, 0f)));
			level.PostDraw(SpriteBatch, spriteFont, tr, 1f);
			base.GraphicsDevice.Viewport = viewport;
			SpriteBatch.End();
			SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, base.GraphicsDevice.SamplerStates[0], null, RasterizerState.CullCounterClockwise, null);
		}
	}

	private bool HasStereo3D()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		for (int i = 0; i < 3; i++)
		{
			PCSaveDevice val = new PCSaveDevice("FEZ");
			string text = "SaveSlot" + i;
			SaveData saveData = null;
			if (val.Load(text, (LoadAction)delegate(BinaryReader stream)
			{
				saveData = SaveFileOperations.Read(new CrcReader(stream));
			}) && saveData != null && saveData.HasStereo3D)
			{
				return true;
			}
		}
		return false;
	}
}
