namespace Moonglade.Configuration.Tests;

public class CommentSettingsTests
{
    [Fact]
    public void DefaultValue_DisablesMarkdownLinks()
    {
        Assert.False(CommentSettings.DefaultValue.EnableMarkdownLinks);
    }

    [Fact]
    public void LoadFromConfig_WhenMarkdownLinksSettingIsMissing_DisablesLinksAndPreservesOtherSettings()
    {
        var config = new BlogConfig();

        config.LoadFromConfig(new Dictionary<string, string>
        {
            [nameof(CommentSettings)] = """{"enableComments":false,"requireCommentReview":true}"""
        });

        Assert.False(config.CommentSettings.EnableMarkdownLinks);
        Assert.False(config.CommentSettings.EnableComments);
        Assert.True(config.CommentSettings.RequireCommentReview);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToJson_RoundTripsMarkdownLinksSetting(bool enableLinks)
    {
        var settings = new CommentSettings { EnableMarkdownLinks = enableLinks };

        var restored = settings.ToJson().FromJson<CommentSettings>();

        Assert.Equal(enableLinks, restored.EnableMarkdownLinks);
    }
}
