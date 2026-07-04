namespace Moonglade.Moderation;

public interface ILocalModerationService
{
    string ModerateContent(string input);
    bool HasBadWords(params string[] input);
}

public class LocalModerationService : ILocalModerationService
{
    private readonly IModerationKeywordProvider _keywordProvider;

    public LocalModerationService(IModerationKeywordProvider keywordProvider)
    {
        _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
    }

    public LocalModerationService(string keywords) : this(new StaticModerationKeywordProvider(keywords))
    {
    }

    public string ModerateContent(string input) => CreateWordFilter().ModerateContent(input);

    public bool HasBadWords(params string[] input) => CreateWordFilter().HasBadWord(input);

    private LocalWordFilter CreateWordFilter() => new(_keywordProvider.GetKeywords() ?? string.Empty);
}
