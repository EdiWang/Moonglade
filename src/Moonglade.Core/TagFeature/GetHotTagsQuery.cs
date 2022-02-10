using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetHotTagsQuery(int Top) : IRequest<IReadOnlyList<KeyValuePair<Tag, int>>>;

public class GetHotTagsQueryHandler : IRequestHandler<GetHotTagsQuery, IReadOnlyList<KeyValuePair<Tag, int>>>
{
    private readonly IRepository<TagEntity> _tagRepo;

    public GetHotTagsQueryHandler(IRepository<TagEntity> tagRepo)
    {
        _tagRepo = tagRepo;
    }

    public async Task<IReadOnlyList<KeyValuePair<Tag, int>>> Handle(GetHotTagsQuery request, CancellationToken cancellationToken)
    {
        if (!_tagRepo.Any()) return new List<KeyValuePair<Tag, int>>();

        var spec = new TagSpec(request.Top);
        var tags = await _tagRepo.SelectAsync(spec, t =>
            new KeyValuePair<Tag, int>(new()
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            }, t.Posts.Count));

        return tags;
    }
}