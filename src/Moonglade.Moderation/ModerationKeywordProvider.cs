namespace Moonglade.Moderation;

public interface IModerationKeywordProvider
{
    string GetKeywords();
}

internal sealed class EmptyModerationKeywordProvider : IModerationKeywordProvider
{
    public string GetKeywords() => string.Empty;
}
