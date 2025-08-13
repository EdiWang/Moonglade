using System.Text;
using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static class BlogTagHelper
{
    public static Dictionary<string, string> TagNormalizationDictionary => new()
    {
        { ".", "-" },
        { "#", "-sharp" },
        { " ", "-" },
        { "+", "-plus" }
    };

    public static string NormalizeName(string orgTagName, IDictionary<string, string> normalizations)
    {
        var isEnglishName = Regex.IsMatch(orgTagName, @"^[a-zA-Z 0-9\.\-\+\#\s]*$");
        if (isEnglishName)
        {
            // special case
            if (orgTagName.StartsWith(".net", StringComparison.OrdinalIgnoreCase))
            {
                orgTagName = orgTagName.ToLowerInvariant().Replace(".net", "dot-net");
            }

            var result = new StringBuilder(orgTagName);
            foreach (var (key, value) in normalizations)
            {
                result.Replace(key, value);
            }
            return result.ToString().ToLower();
        }

        var bytes = Encoding.Unicode.GetBytes(orgTagName);
        var hexArray = bytes.Select(b => $"{b:x2}");
        var hexName = string.Join('-', hexArray);

        return hexName;
    }

    public static bool IsValidTagName(string tagDisplayName)
    {
        if (string.IsNullOrWhiteSpace(tagDisplayName)) return false;

        // Regex performance best practice
        // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices

        const string pattern = @"^[a-zA-Z 0-9\.\-\+\#\s]*$";
        var isEng = Regex.IsMatch(tagDisplayName, pattern);
        if (isEng) return true;

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions#supported-named-blocks
        const string chsPattern = @"^[\p{IsCJKUnifiedIdeographs}]*$";
        var isChs = Regex.IsMatch(tagDisplayName, chsPattern);

        return isChs;
    }

}
