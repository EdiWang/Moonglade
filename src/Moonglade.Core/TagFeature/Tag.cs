using System.Text.RegularExpressions;

namespace Moonglade.Core.TagFeature;

public class Tag
{
    public string DisplayName { get; set; }

    public string NormalizedName { get; set; }

    public static string NormalizeName(string orgTagName, IDictionary<string, string> normalizations)
    {
        var isEnglishName = Regex.IsMatch(orgTagName, @"^[a-zA-Z 0-9\.\-\+\#\s]*$");
        if (isEnglishName)
        {
            // special case
            if (orgTagName.Equals(".net", StringComparison.OrdinalIgnoreCase))
            {
                return "dot-net";
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
}