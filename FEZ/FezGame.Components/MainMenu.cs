using FezEngine.Structure;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class MainMenu : MenuBase
{
	public static MainMenu Instance;

	private SaveSlotSelectionLevel SaveSlotMenuLevel;

	private MenuLevel RealMenuRoot;

	public bool StartedGame { get; private set; }

	public bool ContinuedGame { get; private set; }

	public bool SellingTime { get; private set; }

	public bool HasBought { get; private set; }

	public bool ReturnedToArcade { get; private set; }

	public MainMenu(Game game)
		: base(game)
	{
		base.UpdateOrder = -10;
		base.DrawOrder = 2010;
		Instance = this;
	}

	protected override void PostInitialize()
	{
		if (!Fez.PublicDemo && base.GameState.SaveSlot == -1)
		{
			SaveSlotMenuLevel = new SaveSlotSelectionLevel();
			MenuLevels.Add(SaveSlotMenuLevel);
			SaveSlotMenuLevel.Parent = MenuRoot;
			nextMenuLevel = SaveSlotMenuLevel;
			SaveSlotMenuLevel.RecoverMainMenu = RecoverMenuRoot;
			RealMenuRoot = MenuRoot;
			MenuRoot = SaveSlotMenuLevel;
			SaveSlotMenuLevel.RunStart = StartNewGame;
		}
		else
		{
			AddTopElements();
		}
	}

	private bool RecoverMenuRoot()
	{
		if (CurrentMenuLevel == null)
		{
			return false;
		}
		MenuRoot = RealMenuRoot;
		AddTopElements();
		ChangeMenuLevel(MenuRoot);
		return true;
	}

	private void AddTopElements()
	{
		MenuItem menuItem = MenuRoot.AddItem("ContinueGame", ContinueGame, 0);
		menuItem.Disabled = base.GameState.SaveData.IsNew || base.GameState.SaveData.Level == null || base.GameState.SaveData.CanNewGamePlus;
		menuItem.Selectable = !menuItem.Disabled;
		if (base.GameState.IsTrialMode || (base.GameState.SaveData.IsNew && !base.GameState.SaveData.CanNewGamePlus))
		{
			MenuRoot.AddItem("StartNewGame", StartNewGame, menuItem.Disabled, 1);
		}
		else
		{
			MenuRoot.AddItem("StartNewGame", delegate
			{
				ChangeMenuLevel(StartNewGameMenu);
			}, menuItem.Disabled, 1);
		}
		if (base.GameState.SaveData.CanNewGamePlus)
		{
			MenuRoot.AddItem("StartNewGamePlus", NewGamePlus, 2);
		}
		MenuRoot.SelectedIndex = (base.GameState.SaveData.CanNewGamePlus ? 2 : ((!MenuRoot.Items[0].Selectable) ? 1 : 0));
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sAppear.Emit();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		Instance = null;
	}

	protected override void StartNewGame()
	{
		base.StartNewGame();
		base.GameState.ClearSaveFile();
		base.GameState.SaveData.IsNew = false;
		if (base.GameState.SaveData.HasNewGamePlus)
		{
			base.GameState.SaveData.Level = "GOMEZ_HOUSE_2D";
		}
		StartedGame = true;
	}

	protected override void ContinueGame()
	{
		sinceSelectorPhaseStarted = 0f;
		selectorPhase = SelectorPhase.Disappear;
		sDisappear.Emit();
		ContinuedGame = true;
	}

	protected override void ReturnToArcade()
	{
		if (base.GameState.IsTrialMode)
		{
			SellingTime = true;
			return;
		}
		sinceSelectorPhaseStarted = 0f;
		selectorPhase = SelectorPhase.Disappear;
		base.GameState.ReturnToArcade();
		ReturnedToArcade = true;
	}

	private void NewGamePlus()
	{
		base.GameState.SaveData.Level = "GOMEZ_HOUSE_2D";
		base.GameState.SaveData.IsNewGamePlus = true;
		sinceSelectorPhaseStarted = 0f;
		selectorPhase = SelectorPhase.Disappear;
		sDisappear.Emit().Persistent = true;
		StartedGame = true;
	}

	protected override bool UpdateEarlyOut()
	{
		if (!ContinuedGame && !StartedGame && !HasBought && !SellingTime)
		{
			return ReturnedToArcade;
		}
		return true;
	}

	protected override bool AllowDismiss()
	{
		return CurrentMenuLevel == SaveSlotMenuLevel;
	}

	public override void Draw(GameTime gameTime)
	{
		base.Draw(gameTime);
	}
}
