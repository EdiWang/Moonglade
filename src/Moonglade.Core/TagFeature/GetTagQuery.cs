using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IQuery<TagEntity>;

public class GetTagQueryHandler(MoongladeRepository<TagEntity> repo) : IQueryHandler<GetTagQuery, TagEntity>
{
    public Task<TagEntity> HandleAsync(GetTagQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new TagByNormalizedNameSpec(request.NormalizedName), ct);
}