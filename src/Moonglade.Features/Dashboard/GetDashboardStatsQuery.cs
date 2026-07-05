using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Dashboard;

public record DashboardStats(
    int YesterdayViews,
    int ThisWeekViews,
    int ThisMonthViews,
    int PublishedPostCount,
    int DraftPostCount,
    int ScheduledPostCount,
    int CategoryCount,
    int TagCount,
    IReadOnlyList<DashboardRecentPost> RecentDrafts,
    IReadOnlyList<DashboardRecentPost> RecentPublishedPosts);

public record DashboardRecentPost(Guid Id, string Title, DateTime DateUtc);

public record GetDashboardStatsQuery(DateTime? UtcNow = null) : IQuery<DashboardStats>;

public class GetDashboardStatsQueryHandler(BlogDbContext db) : IQueryHandler<GetDashboardStatsQuery, DashboardStats>
{
    public async Task<DashboardStats> HandleAsync(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var todayUtc = (request.UtcNow ?? DateTime.UtcNow).Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var yesterdayUtc = todayUtc.AddDays(-1);
        var weekStartUtc = todayUtc.AddDays(-GetDaysSinceMonday(todayUtc));
        var monthStartUtc = new DateTime(todayUtc.Year, todayUtc.Month, 1);
        var viewStartUtc = new[] { yesterdayUtc, weekStartUtc, monthStartUtc }.Min();

        var viewCounts = await db.PostViewDaily.AsNoTracking()
            .Where(v => v.ViewDateUtc >= viewStartUtc && v.ViewDateUtc < tomorrowUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Yesterday = g.Sum(v => v.ViewDateUtc >= yesterdayUtc && v.ViewDateUtc < todayUtc ? v.ViewCount : 0),
                ThisWeek = g.Sum(v => v.ViewDateUtc >= weekStartUtc ? v.ViewCount : 0),
                ThisMonth = g.Sum(v => v.ViewDateUtc >= monthStartUtc ? v.ViewCount : 0)
            })
            .SingleOrDefaultAsync(ct);

        var postCounts = await db.Post.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .GroupBy(p => p.PostStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        var categoryCount = await db.Category.CountAsync(ct);
        var tagCount = await db.Tag.CountAsync(ct);

        var recentDrafts = await db.Post.AsNoTracking()
            .Where(p => !p.IsDeleted && p.PostStatus == PostStatus.Draft)
            .OrderByDescending(p => p.LastModifiedUtc ?? p.CreateTimeUtc)
            .Take(2)
            .Select(p => new DashboardRecentPost(
                p.Id,
                p.Title,
                p.LastModifiedUtc ?? p.CreateTimeUtc))
            .ToListAsync(ct);

        var recentPublishedPosts = await db.Post.AsNoTracking()
            .Where(p => !p.IsDeleted && p.PostStatus == PostStatus.Published && p.PubDateUtc != null)
            .OrderByDescending(p => p.PubDateUtc)
            .Take(2)
            .Select(p => new DashboardRecentPost(
                p.Id,
                p.Title,
                p.PubDateUtc.GetValueOrDefault()))
            .ToListAsync(ct);

        return new DashboardStats(
            viewCounts?.Yesterday ?? 0,
            viewCounts?.ThisWeek ?? 0,
            viewCounts?.ThisMonth ?? 0,
            postCounts.GetValueOrDefault(PostStatus.Published),
            postCounts.GetValueOrDefault(PostStatus.Draft),
            postCounts.GetValueOrDefault(PostStatus.Scheduled),
            categoryCount,
            tagCount,
            recentDrafts,
            recentPublishedPosts);
    }

    private static int GetDaysSinceMonday(DateTime date) => ((int)date.DayOfWeek + 6) % 7;
}
