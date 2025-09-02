namespace Moonglade.Moderation;

public interface ILocalModerationService
{
    string ModerateContent(string input);
    bool HasBadWords(params string[] input);
}

public class LocalModerationService(string keywords) : ILocalModerationService
{
    private readonly LocalWordFilter _wordFilter = new(keywords ?? throw new ArgumentNullException(nameof(keywords)));

    public string ModerateContent(string input) => _wordFilter.ModerateContent(input);

    public bool HasBadWords(params string[] input) => _wordFilter.HasBadWord(input);
}
