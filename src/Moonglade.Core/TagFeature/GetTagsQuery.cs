namespace Moonglade.Core.TagFeature;

public record GetTagsQuery : IRequest<List<Tag>>;

public class GetTagsQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetTagsQuery, List<Tag>>
{
    public Task<List<Tag>> Handle(GetTagsQuery request, CancellationToken ct) => repo.SelectAsync(Tag.EntitySelector, ct);
}