namespace Moonglade.Configuration.Tests;

public class CommentSettingsTests
{
    [Fact]
    public void Parse_WhenMarkdownLinksSettingIsMissing_DisablesLinksAndPreservesOtherSettings()
    {
        var (snapshot, _) = BlogConfig.Parse(new Dictionary<string, string>
        {
            [nameof(CommentSettings)] = """{"enableComments":false,"requireCommentReview":true}"""
        });

        Assert.False(snapshot.CommentSettings.EnableMarkdownLinks);
        Assert.False(snapshot.CommentSettings.EnableComments);
        Assert.True(snapshot.CommentSettings.RequireCommentReview);
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
