using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Tools;

namespace FezGame.Tools;

public static class CreditsText
{
	private static readonly Dictionary<string, string> Fallback;

	static CreditsText()
	{
		Fallback = ServiceHelper.Get<IContentManagerProvider>().Global.Load<Dictionary<string, Dictionary<string, string>>>("Resources/CreditsText")[string.Empty];
	}

	public static string GetString(string tag)
	{
		if (tag == null || !Fallback.TryGetValue(tag, out var value))
		{
			return "[MISSING TEXT]";
		}
		return value;
	}
}
