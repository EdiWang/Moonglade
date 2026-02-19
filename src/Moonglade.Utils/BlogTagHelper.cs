using System.Text;
using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static partial class BlogTagHelper
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
        var isEnglishName = EnglishTagNameRegex().IsMatch(orgTagName);
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

        if (EnglishTagNameRegex().IsMatch(tagDisplayName)) return true;

        return CjkTagNameRegex().IsMatch(tagDisplayName);
    }

    [GeneratedRegex(@"^[a-zA-Z 0-9\.\-\+\#\s]*$")]
    private static partial Regex EnglishTagNameRegex();

    [GeneratedRegex(@"^[\p{IsCJKUnifiedIdeographs}]*$")]
    private static partial Regex CjkTagNameRegex();
}
