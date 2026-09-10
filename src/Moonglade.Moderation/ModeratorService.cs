namespace Moonglade.Moderation;

public interface IModeratorService
{
    string Mask(string input);
    bool Detect(params string[] input);
}

public class MoongladeModeratorService(IModerationKeywordProvider keywordProvider) : IModeratorService
{
    public string Mask(string input) =>
        string.IsNullOrWhiteSpace(input) ? input : CreateWordFilter().ModerateContent(input);

    public bool Detect(params string[] input)
    {
        var validInputs = input?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        return validInputs is { Length: > 0 } && CreateWordFilter().HasBadWord(validInputs);
    }

    private LocalWordFilter CreateWordFilter() => new(keywordProvider.GetKeywords() ?? string.Empty);
}
