using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class PauseMenu : MenuBase
{
	public static StarField Starfield;

	public bool Ready;

	private IntroZoomIn IntroZoomIn;

	public static PauseMenu Instance;

	private bool wasStrict;

	private SoundEffect sStarZoom;

	[ServiceDependency]
	public IGameService GameService { get; private set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { get; private set; }

	public PauseMenu(Game game)
		: base(game)
	{
		base.UpdateOrder = -10;
		base.DrawOrder = 2009;
		Instance = this;
	}

	public static void PreInitialize()
	{
		ServiceHelper.AddComponent(Starfield = new StarField(ServiceHelper.Game));
	}

	protected override void PostInitialize()
	{
		if (Starfield == null)
		{
			ServiceHelper.AddComponent(Starfield = new StarField(base.Game));
		}
		MenuRoot.AddItem("ResumeGame", ResumeGame, 0);
		if (Fez.SpeedRunMode && base.GameState.SaveSlot == 4)
		{
			MenuRoot.AddItem(null, ResetSpeedRun, 1).SuffixText = () => "RESET RUN";
		}
		wasStrict = base.InputManager.StrictRotation;
		base.InputManager.StrictRotation = false;
		base.GameState.SaveToCloud();
	}

	protected override void ResumeGame()
	{
		ServiceHelper.AddComponent(new TileTransition(ServiceHelper.Game)
		{
			ScreenCaptured = delegate
			{
				ServiceHelper.RemoveComponent(this);
			}
		});
		base.Enabled = false;
		sDisappear.Emit().Persistent = true;
	}

	protected override void StartNewGame()
	{
		base.StartNewGame();
		base.GameState.ClearSaveFile();
		if (base.GameState.SaveData.HasNewGamePlus)
		{
			base.GameState.SaveData.HasFPView = false;
			base.GameState.SaveData.Level = "GOMEZ_HOUSE_2D";
		}
		sStarZoom.Emit().Persistent = true;
		StartedNewGame = true;
		StartLoading();
		Starfield.Enabled = true;
		base.GameState.InCutscene = true;
	}

	protected override void ReturnToArcade()
	{
		if (base.GameState.IsTrialMode)
		{
			GameService.EndTrial(forceRestart: true);
			Waiters.Wait(0.10000000149011612, delegate
			{
				ServiceHelper.RemoveComponent(this);
			});
		}
		else
		{
			base.GameState.ReturnToArcade();
		}
		base.Enabled = false;
	}

	private void ResetSpeedRun()
	{
		base.GameState.SaveSlot = 4;
		base.GameState.LoadSaveFile(delegate
		{
			base.GameState.Save();
			base.GameState.SaveImmediately();
		});
		SpeedRun.Dispose();
		StartNewGame();
		SpeedRun.Begin(base.CMProvider.Global.Load<Texture2D>("Other Textures/SpeedRun"));
	}

	private void StartLoading()
	{
		base.GameState.Loading = true;
		Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
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
		base.GameState.Loading = true;
		base.GameState.SkipLoadBackground = true;
		base.GameState.Reset();
		base.GameState.UnPause();
		base.GameState.LoadLevel();
		Logger.Log("Pause Menu", "Game restarted.");
		base.GameState.ScheduleLoadEnd = true;
		base.GameState.SkipLoadBackground = false;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sStarZoom = base.CMProvider.Global.Load<SoundEffect>("Sounds/Intro/StarZoom");
		if (!EndGameMenu)
		{
			sAppear.Emit();
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		Instance = null;
		if (Intro.Instance == null && EndCutscene32Host.Instance == null && EndCutscene64Host.Instance == null)
		{
			base.GameState.InCutscene = false;
		}
		base.InputManager.StrictRotation = wasStrict;
	}

	protected override bool UpdateEarlyOut()
	{
		if (base.GameState.IsTrialMode)
		{
			if (StartedNewGame && selectorPhase != SelectorPhase.Disappear)
			{
				sinceSelectorPhaseStarted = 0f;
				selectorPhase = SelectorPhase.Disappear;
			}
			if (StartedNewGame && !base.GameState.Loading)
			{
				DestroyMenu();
				Starfield = null;
				base.CMProvider.Dispose(CM.Intro);
				return true;
			}
		}
		else
		{
			if (StartedNewGame && IntroZoomIn == null && Starfield != null && Starfield.IsDisposed)
			{
				Starfield = null;
				ServiceHelper.AddComponent(IntroZoomIn = new IntroZoomIn(base.Game));
			}
			if (StartedNewGame && IntroZoomIn != null && IntroZoomIn.IsDisposed)
			{
				IntroZoomIn = null;
				base.CMProvider.Dispose(CM.Intro);
				ServiceHelper.RemoveComponent(this);
				return true;
			}
		}
		if ((nextMenuLevel ?? CurrentMenuLevel) == null)
		{
			DestroyMenu();
			return true;
		}
		if (StartedNewGame)
		{
			return true;
		}
		return false;
	}

	protected override bool AllowDismiss()
	{
		return true;
	}

	private void DestroyMenu()
	{
		DestroyMenu(viaSignOut: true);
	}

	private void DestroyMenu(bool viaSignOut)
	{
		if (viaSignOut)
		{
			ServiceHelper.RemoveComponent(this);
		}
		else if (base.Enabled)
		{
			ServiceHelper.AddComponent(new TileTransition(ServiceHelper.Game)
			{
				ScreenCaptured = delegate
				{
					ServiceHelper.RemoveComponent(this);
				}
			});
			base.Enabled = false;
			nextMenuLevel = (CurrentMenuLevel = null);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		Ready = true;
		if (IntroZoomIn == null)
		{
			base.TargetRenderer.DrawFullscreen(Color.Black);
		}
		if (Starfield != null && !Starfield.IsDisposed)
		{
			Starfield.Draw();
		}
		base.Draw(gameTime);
	}

	protected override bool AlwaysShowBackButton()
	{
		return true;
	}
}
