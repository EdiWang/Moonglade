namespace Moonglade.Moderation;

public interface IModerationKeywordProvider
{
    string GetKeywords();
}

internal sealed class EmptyModerationKeywordProvider : IModerationKeywordProvider
{
    public string GetKeywords() => string.Empty;
}

internal sealed class StaticModerationKeywordProvider(string keywords) : IModerationKeywordProvider
{
    private readonly string _keywords = keywords ?? throw new ArgumentNullException(nameof(keywords));

    public string GetKeywords() => _keywords;
}
