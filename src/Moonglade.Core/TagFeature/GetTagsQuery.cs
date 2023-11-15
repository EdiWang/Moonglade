namespace Moonglade.Core.TagFeature;

public record GetTagsQuery : IRequest<IReadOnlyList<Tag>>;

public class GetTagsQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetTagsQuery, IReadOnlyList<Tag>>
{
    public Task<IReadOnlyList<Tag>> Handle(GetTagsQuery request, CancellationToken ct) => repo.SelectAsync(Tag.EntitySelector, ct);
}