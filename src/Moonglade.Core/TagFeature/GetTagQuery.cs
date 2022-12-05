using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<Tag>;

public class GetTagQueryHandler : IRequestHandler<GetTagQuery, Tag>
{
    private readonly IRepository<TagEntity> _repo;

    public GetTagQueryHandler(IRepository<TagEntity> repo) => _repo = repo;

    public Task<Tag> Handle(GetTagQuery request, CancellationToken ct) =>
        _repo.FirstOrDefaultAsync(new TagSpec(request.NormalizedName), Tag.EntitySelector);
}