using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Tools;

namespace FezGame.Tools;

public static class GameText
{
	private static readonly Dictionary<string, string> Fallback;

	private static readonly Dictionary<string, Dictionary<string, string>> AllResources;

	static GameText()
	{
		AllResources = ServiceHelper.Get<IContentManagerProvider>().Global.Load<Dictionary<string, Dictionary<string, string>>>("Resources/GameText");
		Fallback = AllResources[string.Empty];
	}

	public static string GetString(string tag)
	{
		string twoLetterISOLanguageName = Culture.TwoLetterISOLanguageName;
		if (!AllResources.TryGetValue(twoLetterISOLanguageName, out var value))
		{
			value = Fallback;
		}
		if ((tag == null || !value.TryGetValue(tag, out var value2)) && (tag == null || !Fallback.TryGetValue(tag, out value2)))
		{
			return "[MISSING TEXT]";
		}
		return value2;
	}

	public static string GetStringRaw(string tag)
	{
		if (tag == null || !Fallback.TryGetValue(tag, out var value))
		{
			return "[MISSING TEXT]";
		}
		return value;
	}
}
