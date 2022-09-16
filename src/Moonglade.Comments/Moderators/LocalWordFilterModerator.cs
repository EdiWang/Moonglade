using Edi.WordFilter;
using Moonglade.Configuration;

namespace Moonglade.Comments.Moderators;

public class LocalWordFilterModerator : ICommentModerator
{
    private readonly IMaskWordFilter _filter;

    public LocalWordFilterModerator(IBlogConfig blogConfig)
    {
        var sw = new StringWordSource(blogConfig.ContentSettings.DisharmonyWords);
        _filter = new MaskWordFilter(sw);
    }

    public Task<string> ModerateContent(string input) => Task.FromResult(_filter.FilterContent(input));

    public Task<bool> HasBadWord(params string[] input) => Task.FromResult(input.Any(s => _filter.ContainsAnyWord(s)));
}