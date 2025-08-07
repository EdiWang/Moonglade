using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record GetNextScheduledPostTimeQuery : IQuery<DateTime?>;

public class GetNextScheduledPostTimeQueryHandler(MoongladeRepository<PostEntity> postRepo) :
    IQueryHandler<GetNextScheduledPostTimeQuery, DateTime?>
{
    public async Task<DateTime?> HandleAsync(GetNextScheduledPostTimeQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextPost = await postRepo.FirstOrDefaultAsync(new NextScheduledPostSpec(now), ct);
        return nextPost?.ScheduledPublishTimeUtc;
    }
}