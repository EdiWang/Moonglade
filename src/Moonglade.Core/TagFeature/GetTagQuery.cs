using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<TagEntity>;

public class GetTagQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetTagQuery, TagEntity>
{
    public Task<TagEntity> Handle(GetTagQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new TagByNormalizedNameSpec(request.NormalizedName), ct);
}