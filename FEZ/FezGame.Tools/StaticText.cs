using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Tools;

namespace FezGame.Tools;

public static class StaticText
{
	private static readonly Dictionary<string, string> Fallback;

	private static readonly Dictionary<string, Dictionary<string, string>> AllResources;

	static StaticText()
	{
		AllResources = ServiceHelper.Get<IContentManagerProvider>().Global.Load<Dictionary<string, Dictionary<string, string>>>("Resources/StaticText");
		Fallback = AllResources[string.Empty];
	}

	public static bool TryGetString(string tag, out string text)
	{
		string twoLetterISOLanguageName = Culture.TwoLetterISOLanguageName;
		if (!AllResources.TryGetValue(twoLetterISOLanguageName, out var value))
		{
			value = Fallback;
		}
		if ((tag == null || !value.TryGetValue(tag, out text)) && (tag == null || !Fallback.TryGetValue(tag, out text)))
		{
			text = "[MISSING TEXT]";
			return false;
		}
		return true;
	}

	public static string GetString(string tag)
	{
		if (TryGetString(tag, out var text))
		{
			return text;
		}
		return "[MISSING TEXT]";
	}
}
