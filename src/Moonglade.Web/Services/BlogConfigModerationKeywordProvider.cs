using Moonglade.Moderation;

namespace Moonglade.Web.Services;

public class BlogConfigModerationKeywordProvider(IBlogConfig blogConfig) : IModerationKeywordProvider
{
    public string GetKeywords() => blogConfig.CommentSettings.WordFilterKeywords ?? string.Empty;
}
