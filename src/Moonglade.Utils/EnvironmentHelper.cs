using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static class EnvironmentHelper
{
    public static bool IsRunningOnAzureAppService() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));

    public static bool IsRunningInDocker() => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    /// <summary>
    /// Get values from `MOONGLADE_TAGS` Environment Variable
    /// </summary>
    /// <returns>string values</returns>
    public static IEnumerable<string> GetEnvironmentTags()
    {
        var tagsEnv = Environment.GetEnvironmentVariable("MOONGLADE_TAGS");
        if (string.IsNullOrWhiteSpace(tagsEnv))
        {
            yield return string.Empty;
            yield break;
        }

        var tagRegex = new Regex(@"^[a-zA-Z0-9-#@$()\[\]/]+$");
        var tags = tagsEnv.Split(',');
        foreach (string tag in tags)
        {
            var t = tag.Trim();
            if (tagRegex.IsMatch(t))
            {
                yield return t;
            }
        }
    }
}
