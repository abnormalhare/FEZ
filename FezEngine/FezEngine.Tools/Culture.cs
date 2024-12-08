using System;
using System.Globalization;

namespace FezEngine.Tools;

public static class Culture
{
	public static Language Language = LanguageFromCurrentCulture();

	public static bool IsCJK => Language.IsCjk();

	public static string TwoLetterISOLanguageName => Language switch
	{
		Language.English => "en", 
		Language.French => "fr", 
		Language.Italian => "it", 
		Language.German => "de", 
		Language.Spanish => "es", 
		Language.Portuguese => "pt", 
		Language.Chinese => "zh", 
		Language.Japanese => "ja", 
		Language.Korean => "ko", 
		_ => throw new InvalidOperationException("Unknown culture"), 
	};

	public static Language LanguageFromCurrentCulture()
	{
		return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
		{
			"fr" => Language.French, 
			"it" => Language.Italian, 
			"de" => Language.German, 
			"es" => Language.Spanish, 
			"pt" => Language.Portuguese, 
			"zh" => Language.Chinese, 
			"ja" => Language.Japanese, 
			"ko" => Language.Korean, 
			_ => Language.English, 
		};
	}

	public static bool IsCjk(this Language language)
	{
		if (language != Language.Japanese && language != Language.Korean)
		{
			return language == Language.Chinese;
		}
		return true;
	}
}
