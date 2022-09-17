namespace Moonglade.Core.TagFeature;

public record GetTagsQuery : IRequest<IReadOnlyList<Tag>>;

public class GetTagsQueryHandler : IRequestHandler<GetTagsQuery, IReadOnlyList<Tag>>
{
    private readonly IRepository<TagEntity> _repo;


    public GetTagsQueryHandler(IRepository<TagEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<Tag>> Handle(GetTagsQuery request, CancellationToken ct) => _repo.SelectAsync(Tag.EntitySelector);
}