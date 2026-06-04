namespace Moonglade.Configuration.Tests;

public class AdvancedSettingsTests
{
    [Fact]
    public void DefaultValue_UsesSeparateExternalLinkWarningDefaults()
    {
        var settings = AdvancedSettings.DefaultValue;

        Assert.False(settings.WarnPostExternalLink);
        Assert.True(settings.WarnCommentExternalLink);
    }

    [Fact]
    public void DefaultValueJson_UsesSeparateExternalLinkWarningDefaults()
    {
        var settings = AdvancedSettings.DefaultValue.ToJson().FromJson<AdvancedSettings>();

        Assert.False(settings.WarnPostExternalLink);
        Assert.True(settings.WarnCommentExternalLink);
    }

    [Fact]
    public void FromJson_WhenExternalLinkWarningSettingsAreMissing_UsesSeparateDefaults()
    {
        var settings = "{}".FromJson<AdvancedSettings>();

        Assert.False(settings.WarnPostExternalLink);
        Assert.True(settings.WarnCommentExternalLink);
    }
}
