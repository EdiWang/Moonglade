using Moonglade.Data;

namespace Moonglade.Core.TagFeature;

public record GetTagsQuery : IRequest<List<TagEntity>>;

public class GetTagsQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetTagsQuery, List<TagEntity>>
{
    public Task<List<TagEntity>> Handle(GetTagsQuery request, CancellationToken ct) => repo.ListAsync(ct);
}