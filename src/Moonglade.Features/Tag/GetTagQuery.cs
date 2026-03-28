using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Tag;

public record GetTagQuery(string NormalizedName) : IQuery<TagEntity>;

public class GetTagQueryHandler(BlogDbContext db) : IQueryHandler<GetTagQuery, TagEntity>
{
    public Task<TagEntity> HandleAsync(GetTagQuery request, CancellationToken ct) =>
        db.Tag.FirstOrDefaultAsync(t => t.NormalizedName == request.NormalizedName, ct);
}