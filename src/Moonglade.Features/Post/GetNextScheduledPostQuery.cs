using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Post;

public record GetNextScheduledPostTimeQuery : IQuery<DateTime?>;

public class GetNextScheduledPostTimeQueryHandler(BlogDbContext db) :
    IQueryHandler<GetNextScheduledPostTimeQuery, DateTime?>
{
    public async Task<DateTime?> HandleAsync(GetNextScheduledPostTimeQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await db.Post
            .AsNoTracking()
            .Where(p => p.PostStatus == PostStatus.Scheduled && p.ScheduledPublishTimeUtc > now)
            .OrderBy(p => p.ScheduledPublishTimeUtc)
            .Select(p => p.ScheduledPublishTimeUtc)
            .FirstOrDefaultAsync(ct);
    }
}