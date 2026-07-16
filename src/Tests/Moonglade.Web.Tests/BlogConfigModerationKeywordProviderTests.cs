using Moonglade.Configuration;
using Moonglade.Web.Services;

namespace Moonglade.Web.Tests;

public class BlogConfigModerationKeywordProviderTests
{
    [Fact]
    public void GetKeywords_ReturnsCommentSettingsWordFilterKeywords()
    {
        // Arrange
        var blogConfig = new BlogConfig
        {
            CommentSettings = new CommentSettings
            {
                WordFilterKeywords = "spam|scam"
            }
        };
        var provider = new BlogConfigModerationKeywordProvider(blogConfig);

        // Act
        var keywords = provider.GetKeywords();

        // Assert
        Assert.Equal("spam|scam", keywords);
    }

    [Fact]
    public void GetKeywords_WhenSettingIsNull_ReturnsEmptyString()
    {
        // Arrange
        var blogConfig = new BlogConfig
        {
            CommentSettings = new CommentSettings
            {
                WordFilterKeywords = null!
            }
        };
        var provider = new BlogConfigModerationKeywordProvider(blogConfig);

        // Act
        var keywords = provider.GetKeywords();

        // Assert
        Assert.Equal(string.Empty, keywords);
    }
}
