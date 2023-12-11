using System.Globalization;

using Moonglade.Data.Enum;

namespace Moonglade.Data;

public class DataHelper
{
	public static LanguageEnum GetLanguage()
	{
		string culture = CultureInfo.CurrentCulture.Name;

		switch (culture)
		{
			case "de-DE":
				return LanguageEnum.German;
			case "en-US":
				return LanguageEnum.English;
			default:
				return LanguageEnum.Unknown;
		}
	}

}
