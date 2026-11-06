namespace Moonglade.Moderation;

public interface IModeratorService
{
    string Mask(string input);
    bool Detect(params string[] input);
    string Mask(string input, string keywords);
    bool Detect(string[] input, string keywords);
}

public class MoongladeModeratorService(IModerationKeywordProvider keywordProvider) : IModeratorService
{
    public string Mask(string input) =>
        string.IsNullOrWhiteSpace(input) ? input : CreateWordFilter().ModerateContent(input);

    public string Mask(string input, string keywords) =>
        string.IsNullOrWhiteSpace(input) ? input : new LocalWordFilter(keywords ?? string.Empty).ModerateContent(input);

    public bool Detect(string[] input, string keywords)
    {
        var validInputs = input?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        return validInputs is { Length: > 0 } && new LocalWordFilter(keywords ?? string.Empty).HasBadWord(validInputs);
    }

    public bool Detect(params string[] input)
    {
        var validInputs = input?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        return validInputs is { Length: > 0 } && CreateWordFilter().HasBadWord(validInputs);
    }

    private LocalWordFilter CreateWordFilter() => new(keywordProvider.GetKeywords() ?? string.Empty);
}
