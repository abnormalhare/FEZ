using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class GameStateControl : GameComponent
{
	private const float SaveWaitTimeSeconds = 4f;

	private IWaiter loadWaiter;

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	public GameStateControl(Game game)
		: base(game)
	{
		base.UpdateOrder = -3;
	}

	public override void Initialize()
	{
		base.Game.Deactivated += delegate(object s, EventArgs ea)
		{
			if (SettingsManager.Settings.PauseOnLostFocus)
			{
				if (!GameState.SkipRendering)
				{
					DoPause(s, ea);
				}
				ServiceHelper.Get<ISoundManager>().GlobalVolumeFactor = 0f;
			}
		};
		base.Game.Activated += delegate
		{
			if (SettingsManager.Settings.PauseOnLostFocus)
			{
				ServiceHelper.Get<ISoundManager>().GlobalVolumeFactor = 1f;
			}
		};
		InputManager.ActiveControllerDisconnected += delegate
		{
			if (SettingsManager.Settings.PauseOnLostFocus)
			{
				DoPause(null, EventArgs.Empty);
			}
		};
		GameState.DynamicUpgrade += DynamicUpgrade;
	}

	private void DynamicUpgrade()
	{
		GameState.ForcedSignOut = true;
		GameState.Restart();
	}

	private void DoPause(object s, EventArgs ea)
	{
		bool checkActive = s == base.Game;
		if (loadWaiter != null)
		{
			return;
		}
		loadWaiter = Waiters.Wait(() => !GameState.Loading && (!checkActive || Intro.Instance == null), delegate
		{
			loadWaiter = null;
			if ((!checkActive || !base.Game.IsActive) && MainMenu.Instance == null)
			{
				GameState.Pause();
			}
		});
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.SinceSaveRequest != -1f)
		{
			GameState.SinceSaveRequest += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (GameState.SinceSaveRequest > 4f)
			{
				GameState.SaveImmediately();
			}
		}
		if (GameState.Loading || CameraManager.Viewpoint == Viewpoint.Perspective)
		{
			return;
		}
		if ((!GameState.InCutscene || GameState.InEndCutscene) && InputManager.Start == FezButtonState.Pressed)
		{
			GameState.Pause();
		}
		if (!GameState.InCutscene && PlayerManager.CanControl && !GameState.FarawaySettings.InTransition && !PlayerManager.HideFez && PlayerManager.Action != ActionType.OpeningTreasure && PlayerManager.Action != ActionType.OpeningDoor && PlayerManager.Action != ActionType.FindingTreasure && PlayerManager.Action != ActionType.ReadingSign && PlayerManager.Action != ActionType.LesserWarp && PlayerManager.Action != ActionType.GateWarp && !PlayerManager.Action.IsEnteringDoor() && PlayerManager.Action != ActionType.ExitDoor && PlayerManager.Action != ActionType.TurnToBell && PlayerManager.Action != ActionType.TurnAwayFromBell && PlayerManager.Action != ActionType.HitBell && PlayerManager.Action != ActionType.WalkingTo && CameraManager.ViewTransitionReached && !GameState.Paused && !(LevelManager.Name == "ELDERS") && EndCutscene32Host.Instance == null && EndCutscene64Host.Instance == null)
		{
			if (InputManager.OpenInventory == FezButtonState.Pressed && !GameState.IsTrialMode && !GameState.InMenuCube && !LevelManager.Name.StartsWith("GOMEZ_HOUSE_END") && PlayerManager.Action != ActionType.WalkingTo)
			{
				GameState.ToggleInventory();
			}
			if (!GameState.InMap && InputManager.Back == FezButtonState.Pressed && (GameState.SaveData.CanOpenMap || Fez.LevelChooser) && LevelManager.Name != "PYRAMID" && !LevelManager.Name.StartsWith("GOMEZ_HOUSE_END"))
			{
				GameState.ToggleMap();
			}
		}
	}
}
