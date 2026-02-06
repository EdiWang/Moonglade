using LiteBus.Queries.Abstractions;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public record GetNextScheduledPostTimeQuery : IQuery<DateTime?>;

public class GetNextScheduledPostTimeQueryHandler(IRepositoryBase<PostEntity> postRepo) :
    IQueryHandler<GetNextScheduledPostTimeQuery, DateTime?>
{
    public async Task<DateTime?> HandleAsync(GetNextScheduledPostTimeQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextPost = await postRepo.FirstOrDefaultAsync(new NextScheduledPostSpec(now), ct);
        return nextPost?.ScheduledPublishTimeUtc;
    }
}