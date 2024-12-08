using Steamworks;

namespace FezGame.Components;

public class SteamAchievement
{
	public readonly string AchievementName;

	public bool IsAchieved
	{
		get
		{
			bool result = default(bool);
			SteamUserStats.GetAchievement(AchievementName, ref result);
			return result;
		}
	}

	public SteamAchievement(string key)
	{
		AchievementName = key;
	}
}
