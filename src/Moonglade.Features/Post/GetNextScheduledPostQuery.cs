using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Post;

public record GetNextScheduledPostTimeQuery : IQuery<DateTime?>;

public class GetNextScheduledPostTimeQueryHandler(BlogDbContext db) :
    IQueryHandler<GetNextScheduledPostTimeQuery, DateTime?>
{
    public async Task<DateTime?> HandleAsync(GetNextScheduledPostTimeQuery request, CancellationToken ct)
    {
        return await db.Post
            .AsNoTracking()
            .Where(p => p.PostStatus == PostStatus.Scheduled && !p.IsDeleted && p.ScheduledPublishTimeUtc != null)
            .OrderBy(p => p.ScheduledPublishTimeUtc)
            .Select(p => p.ScheduledPublishTimeUtc)
            .FirstOrDefaultAsync(ct);
    }
}
