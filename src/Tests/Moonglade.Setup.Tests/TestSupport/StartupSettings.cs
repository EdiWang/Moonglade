using Moonglade.Configuration;

namespace Moonglade.Setup.Tests.TestSupport;

internal static class StartupSettings
{
    public static Dictionary<string, string> Defaults(bool isNew = true)
    {
        var settings = BlogConfig.Parse(new Dictionary<string, string>()).Defaults;
        settings.Add(nameof(SystemManifestSettings), (isNew ? SystemManifestSettings.DefaultValueNew : SystemManifestSettings.DefaultValue).ToJson());
        return settings;
    }
}
