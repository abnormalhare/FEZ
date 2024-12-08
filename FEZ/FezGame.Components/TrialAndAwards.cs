using System;
using System.Collections.Generic;
using Common;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class TrialAndAwards : GameComponent, IGameService, IScriptingBase
{
	private static readonly TimeSpan CheckFrequency = TimeSpan.FromSeconds(0.25);

	private TimeSpan sinceChecked = CheckFrequency;

	private List<SteamAchievement> Achievements;

	public bool IsMapQrResolved => GameState.SaveData.MapCheatCodeDone;

	public bool IsScrollOpen => GameState.ActiveScroll != null;

	public string GetGlobalState => GameState.SaveData.ScriptingState ?? string.Empty;

	public string GetLevelState => GameState.SaveData.ThisLevel.ScriptingState ?? string.Empty;

	public bool IsSewerQrResolved
	{
		get
		{
			bool num = GameState.SaveData.World.ContainsKey("SEWER_QR") && GameState.SaveData.World["SEWER_QR"].InactiveArtObjects.Contains(0);
			bool flag = GameState.SaveData.World.ContainsKey("ZU_THRONE_RUINS") && GameState.SaveData.World["ZU_THRONE_RUINS"].InactiveVolumes.Contains(2);
			bool flag2 = GameState.SaveData.World.ContainsKey("ZU_HOUSE_EMPTY") && GameState.SaveData.World["ZU_HOUSE_EMPTY"].InactiveVolumes.Contains(2);
			return num || flag2 || flag;
		}
	}

	public bool IsZuQrResolved
	{
		get
		{
			bool num = GameState.SaveData.World.ContainsKey("PARLOR") && GameState.SaveData.World["PARLOR"].InactiveVolumes.Contains(4);
			bool flag = GameState.SaveData.World.ContainsKey("ZU_HOUSE_QR") && GameState.SaveData.World["ZU_HOUSE_QR"].InactiveVolumes.Contains(0);
			return num || flag;
		}
	}

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	public TrialAndAwards(Game game)
		: base(game)
	{
	}

	public void ResetEvents()
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += delegate
		{
			CollisionManager.GravityFactor = 1f;
		};
	}

	public override void Update(GameTime gameTime)
	{
		sinceChecked += gameTime.ElapsedGameTime;
		if (sinceChecked < CheckFrequency)
		{
			return;
		}
		sinceChecked = TimeSpan.Zero;
		bool flag = PauseMenu.Instance != null && PauseMenu.Instance.EndGameMenu;
		if ((GameState.Paused && !flag) || (GameState.InCutscene && !flag) || GameState.Loading)
		{
			return;
		}
		if (!Fez.NoSteamworks && Achievements == null)
		{
			Achievements = new List<SteamAchievement>
			{
				new SteamAchievement("Achievement_01"),
				new SteamAchievement("Achievement_02"),
				new SteamAchievement("Achievement_03"),
				new SteamAchievement("Achievement_04"),
				new SteamAchievement("Achievement_05"),
				new SteamAchievement("Achievement_06"),
				new SteamAchievement("Achievement_07"),
				new SteamAchievement("Achievement_08"),
				new SteamAchievement("Achievement_09"),
				new SteamAchievement("Achievement_10"),
				new SteamAchievement("Achievement_11"),
				new SteamAchievement("Achievement_12")
			};
		}
		if (Achievements == null)
		{
			return;
		}
		foreach (SteamAchievement achievement in Achievements)
		{
			if (!achievement.IsAchieved && CheckAchievement(achievement.AchievementName))
			{
				if (!GameState.SaveData.EarnedAchievements.Contains(achievement.AchievementName))
				{
					GameState.SaveData.EarnedAchievements.Add(achievement.AchievementName);
				}
				GameState.AwardAchievement(achievement);
			}
		}
	}

	public bool CheckAchievement(string key)
	{
		switch (key)
		{
		case "Achievement_01":
			if (GameState.SaveData.SecretCubes >= 32 && GameState.SaveData.CubeShards >= 32)
			{
				return GameState.SaveData.Artifacts.Count >= 4;
			}
			return false;
		case "Achievement_02":
			if (GameState.SaveData.CubeShards >= 1)
			{
				return PlayerManager.CanControl;
			}
			return false;
		case "Achievement_03":
			if (!GameState.SaveData.HasNewGamePlus)
			{
				if (PauseMenu.Instance != null)
				{
					return PauseMenu.Instance.EndGameMenu;
				}
				return false;
			}
			return true;
		case "Achievement_04":
			if (GameState.SaveData.SecretCubes >= 32)
			{
				return GameState.SaveData.CubeShards >= 32;
			}
			return false;
		case "Achievement_05":
			return GameState.SaveData.Artifacts.Contains(ActorType.Tome);
		case "Achievement_06":
			return GameState.SaveData.Artifacts.Contains(ActorType.TriSkull);
		case "Achievement_07":
			return GameState.SaveData.Artifacts.Contains(ActorType.LetterCube);
		case "Achievement_08":
			return GameState.SaveData.Artifacts.Contains(ActorType.NumberCube);
		case "Achievement_09":
			return GameState.SaveData.SecretCubes > 0;
		case "Achievement_10":
			return GameState.SaveData.UnlockedWarpDestinations.Count >= 5;
		case "Achievement_11":
			return GameState.SaveData.AnyCodeDeciphered;
		case "Achievement_12":
			return GameState.SaveData.AchievementCheatCodeDone;
		default:
			return false;
		}
	}

	public void EndTrial(bool forceRestart)
	{
		if (!GameState.IsTrialMode)
		{
			return;
		}
		ScreenFade obj = new ScreenFade(ServiceHelper.Game)
		{
			FromColor = ColorEx.TransparentBlack,
			ToColor = Color.Black,
			EaseOut = true,
			CaptureScreen = true,
			Duration = 1f,
			DrawOrder = 2050,
			WaitUntil = () => !GameState.Loading
		};
		obj.ScreenCaptured = (Action)Delegate.Combine(obj.ScreenCaptured, (Action)delegate
		{
			GameState.SkipLoadBackground = true;
			GameState.Loading = true;
			Worker<bool> worker = ThreadPool.Take<bool>(DoSellScreen);
			worker.Finished += delegate
			{
				ThreadPool.Return(worker);
				GameState.ScheduleLoadEnd = true;
				GameState.SkipLoadBackground = false;
			};
			worker.Start(forceRestart);
		});
		ServiceHelper.AddComponent(obj);
	}

	private void DoSellScreen(bool forceRestart)
	{
		if (forceRestart)
		{
			GameState.Reset();
		}
		if (GameState.InCutscene && Intro.Instance != null)
		{
			ServiceHelper.RemoveComponent(Intro.Instance);
		}
		Intro obj = new Intro(base.Game)
		{
			Sell = true,
			FadeBackToGame = !forceRestart
		};
		ServiceHelper.AddComponent(obj);
		obj.LoadVideo();
	}

	public LongRunningAction Wait(float seconds)
	{
		return new LongRunningAction((float elapsed, float sinceStarted) => sinceStarted >= seconds);
	}

	public LongRunningAction GlitchUp()
	{
		NesGlitches nesGlitches = new NesGlitches(base.Game);
		ServiceHelper.AddComponent(nesGlitches);
		bool disposed = false;
		nesGlitches.Disposed += delegate
		{
			disposed = true;
		};
		return new LongRunningAction((float _, float __) => disposed);
	}

	public LongRunningAction Reboot(string toLevel)
	{
		Reboot reboot = new Reboot(base.Game, toLevel);
		ServiceHelper.AddComponent(reboot);
		bool disposed = false;
		reboot.Disposed += delegate
		{
			disposed = true;
		};
		return new LongRunningAction((float _, float __) => disposed);
	}

	public void SetGravity(bool inverted, float factor)
	{
		if (factor == 0f)
		{
			factor = 1f;
		}
		factor = Math.Abs(factor);
		CollisionManager.GravityFactor = (inverted ? (0f - factor) : factor);
	}

	public void AllowMapUsage()
	{
		GameState.SaveData.CanOpenMap = true;
	}

	public void ShowCapsuleLetter()
	{
		ServiceHelper.AddComponent(new GeezerLetterSender(base.Game));
	}

	public LongRunningAction ShowScroll(string localizedString, float forSeconds, bool onTop, bool onVolume)
	{
		if (GameState.ActiveScroll != null)
		{
			TextScroll textScroll = GameState.ActiveScroll;
			while (textScroll.NextScroll != null)
			{
				textScroll = textScroll.NextScroll;
			}
			if (textScroll.Key == localizedString && !textScroll.Closing)
			{
				return null;
			}
			textScroll.Closing = true;
			textScroll.Timeout = null;
			TextScroll nextScroll = new TextScroll(base.Game, GameText.GetString(localizedString), onTop)
			{
				Key = localizedString
			};
			if (forSeconds > 0f)
			{
				nextScroll.Timeout = forSeconds;
			}
			GameState.ActiveScroll.NextScroll = nextScroll;
			if (onVolume)
			{
				return new LongRunningAction(delegate
				{
					CloseScroll(nextScroll);
				});
			}
			return null;
		}
		TextScroll textScroll2 = new TextScroll(base.Game, GameText.GetString(localizedString), onTop)
		{
			Key = localizedString
		};
		TextScroll component = (GameState.ActiveScroll = textScroll2);
		ServiceHelper.AddComponent(component);
		if (forSeconds > 0f)
		{
			GameState.ActiveScroll.Timeout = forSeconds;
		}
		if (onVolume)
		{
			return new LongRunningAction(delegate
			{
				CloseScroll(localizedString);
			});
		}
		return null;
	}

	private void CloseScroll(TextScroll scroll)
	{
		if (GameState.ActiveScroll == null)
		{
			return;
		}
		if (GameState.ActiveScroll == scroll)
		{
			GameState.ActiveScroll.Close();
			return;
		}
		TextScroll textScroll = GameState.ActiveScroll;
		for (TextScroll nextScroll = textScroll.NextScroll; nextScroll != null; nextScroll = nextScroll.NextScroll)
		{
			if (nextScroll == scroll)
			{
				textScroll.NextScroll = nextScroll.NextScroll;
				break;
			}
			textScroll = nextScroll;
		}
	}

	public void CloseScroll(string key)
	{
		if (GameState.ActiveScroll == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(key))
		{
			GameState.ActiveScroll.Close();
			GameState.ActiveScroll.NextScroll = null;
			return;
		}
		if (GameState.ActiveScroll.Key == key)
		{
			GameState.ActiveScroll.Close();
			return;
		}
		TextScroll textScroll = GameState.ActiveScroll;
		for (TextScroll nextScroll = textScroll.NextScroll; nextScroll != null; nextScroll = nextScroll.NextScroll)
		{
			if (nextScroll.Key == key)
			{
				textScroll.NextScroll = nextScroll.NextScroll;
				break;
			}
			textScroll = nextScroll;
		}
	}

	public void SetGlobalState(string state)
	{
		GameState.SaveData.ScriptingState = state;
		GameState.Save();
	}

	public void SetLevelState(string state)
	{
		GameState.SaveData.ThisLevel.ScriptingState = state;
		GameState.Save();
	}

	public void ResolveMapQR()
	{
		GameState.SaveData.MapCheatCodeDone = true;
		GameState.Save();
	}

	public void ResolveSewerQR()
	{
		if (GameState.SaveData.World.ContainsKey("SEWER_QR") && !GameState.SaveData.World["SEWER_QR"].InactiveArtObjects.Contains(0))
		{
			GameState.SaveData.World["SEWER_QR"].InactiveArtObjects.Add(0);
			GameState.SaveData.World["SEWER_QR"].FilledConditions.SecretCount++;
		}
		if (GameState.SaveData.World.ContainsKey("ZU_THRONE_RUINS") && !GameState.SaveData.World["ZU_THRONE_RUINS"].InactiveVolumes.Contains(2))
		{
			GameState.SaveData.World["ZU_THRONE_RUINS"].InactiveVolumes.Add(2);
			GameState.SaveData.World["ZU_THRONE_RUINS"].FilledConditions.SecretCount++;
		}
		if (GameState.SaveData.World.ContainsKey("ZU_HOUSE_EMPTY") && !GameState.SaveData.World["ZU_HOUSE_EMPTY"].InactiveVolumes.Contains(2))
		{
			GameState.SaveData.World["ZU_HOUSE_EMPTY"].InactiveVolumes.Add(2);
			GameState.SaveData.World["ZU_HOUSE_EMPTY"].FilledConditions.SecretCount++;
		}
		GameState.Save();
	}

	public void ResolveZuQR()
	{
		if (GameState.SaveData.World.ContainsKey("PARLOR") && !GameState.SaveData.World["PARLOR"].InactiveVolumes.Contains(4))
		{
			GameState.SaveData.World["PARLOR"].InactiveVolumes.Add(4);
			GameState.SaveData.World["PARLOR"].FilledConditions.SecretCount++;
		}
		if (GameState.SaveData.World.ContainsKey("ZU_HOUSE_QR") && !GameState.SaveData.World["ZU_HOUSE_QR"].InactiveVolumes.Contains(0))
		{
			GameState.SaveData.World["ZU_HOUSE_QR"].InactiveVolumes.Add(0);
			GameState.SaveData.World["ZU_HOUSE_QR"].FilledConditions.SecretCount++;
		}
		GameState.Save();
	}

	public void Start32BitCutscene()
	{
		SpeedRun.CallTime(Util.LocalSaveFolder);
		ServiceHelper.AddComponent(new EndCutscene32Host(base.Game));
	}

	public void Start64BitCutscene()
	{
		SpeedRun.CallTime(Util.LocalSaveFolder);
		ServiceHelper.AddComponent(new EndCutscene64Host(base.Game));
	}

	public void Checkpoint()
	{
		Waiters.Wait(() => PlayerManager.Grounded && PlayerManager.Ground.First.Trile.ActorSettings.Type.IsSafe(), delegate
		{
			if (LevelManager.Name == "LAVA" && LevelManager.WaterHeight < 50f)
			{
				LevelManager.WaterHeight = 132f;
			}
			PlayerManager.RecordRespawnInformation(markCheckpoint: true);
		});
	}
}
