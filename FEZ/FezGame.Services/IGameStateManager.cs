using System;
using EasyStorage;
using FezEngine.Services;
using FezGame.Components;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public interface IGameStateManager : IEngineStateManager
{
	int SaveSlot { get; set; }

	SaveData SaveData { get; set; }

	ISaveDevice ActiveSaveDevice { get; set; }

	bool Saving { get; }

	bool LoadingVisible { get; set; }

	bool ForceLoadIcon { get; set; }

	float SinceSaveRequest { get; set; }

	bool ShowDebuggingBag { get; set; }

	bool JetpackMode { get; set; }

	bool DebugMode { get; set; }

	bool SkipFadeOut { get; set; }

	bool InCutscene { get; set; }

	bool ForcedSignOut { get; set; }

	bool InEndCutscene { get; set; }

	bool EndGame { get; set; }

	PlayerIndex ActivePlayer { get; }

	SteamUser ActiveGamer { get; }

	bool HasActivePlayer { get; }

	string LoggedOutPlayerTag { get; set; }

	bool IsTrialMode { get; }

	new bool InMap { get; set; }

	new bool InMenuCube { get; set; }

	bool MenuCubeIsZoomed { get; set; }

	bool SkipLoadBackground { get; set; }

	bool SkipLoadScreen { get; set; }

	bool HideHUD { get; set; }

	TextScroll ActiveScroll { get; set; }

	bool ForceTimePaused { get; set; }

	bool DisallowRotation { get; set; }

	bool ScheduleLoadEnd { get; set; }

	bool IsAchievementSave { get; set; }

	event Action LiveConnectionChanged;

	event Action DynamicUpgrade;

	event Action HudElementChanged;

	void OnHudElementChanged();

	void AwardAchievement(SteamAchievement achievement);

	void UnPause();

	void Pause();

	void Pause(bool toCredits);

	void ToggleInventory();

	void ToggleMap();

	void Restart();

	void ClearSaveFile();

	void LoadSaveFile(Action onFinish);

	void LoadLevel();

	void LoadLevelAsync(Action onFinish);

	void SignInAndChooseStorage(Action onFinish);

	void Save();

	void SaveToCloud(bool force = false);

	void SaveImmediately(bool ngpBackup = false);

	void StartNewGame(Action onFinish);

	void Reset();

	void ReturnToArcade();

	void ShowScroll(string actualString, float forSeconds, bool onTop);
}
