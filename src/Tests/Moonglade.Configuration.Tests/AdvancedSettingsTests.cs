namespace Moonglade.Configuration.Tests;

public class AdvancedSettingsTests
{
    [Fact]
    public void Parse_WhenExternalLinkWarningSettingsAreMissing_UsesSeparateDefaults()
    {
        var (snapshot, _) = BlogConfig.Parse(new Dictionary<string, string>
        {
            [nameof(AdvancedSettings)] = "{}"
        });
        var settings = snapshot.AdvancedSettings;

        Assert.False(settings.WarnPostExternalLink);
        Assert.True(settings.WarnCommentExternalLink);
    }
}
