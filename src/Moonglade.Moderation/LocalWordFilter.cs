using Edi.WordFilter;

namespace Moonglade.Moderation;

public class LocalWordFilter
{
    private readonly IMaskWordFilter _filter;

    public LocalWordFilter(string words)
    {
        var sw = new StringWordSource(words);

        IMaskWordFilter filter = new TrieTreeWordFilter(sw);
        _filter = filter;
    }

    public string ModerateContent(string input) => _filter.FilterContent(input);

    public bool HasBadWord(params string[] input) => input.Any(s => _filter.ContainsAnyWord(s));
}
