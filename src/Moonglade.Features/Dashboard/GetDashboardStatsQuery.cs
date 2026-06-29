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
    int TagCount);

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

        var yesterdayViews = await SumViewsAsync(yesterdayUtc, todayUtc, ct);
        var thisWeekViews = await SumViewsAsync(weekStartUtc, tomorrowUtc, ct);
        var thisMonthViews = await SumViewsAsync(monthStartUtc, tomorrowUtc, ct);

        var postCounts = await db.Post.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .GroupBy(p => p.PostStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        var categoryCount = await db.Category.CountAsync(ct);
        var tagCount = await db.Tag.CountAsync(ct);

        return new DashboardStats(
            yesterdayViews,
            thisWeekViews,
            thisMonthViews,
            postCounts.GetValueOrDefault(PostStatus.Published),
            postCounts.GetValueOrDefault(PostStatus.Draft),
            postCounts.GetValueOrDefault(PostStatus.Scheduled),
            categoryCount,
            tagCount);
    }

    private async Task<int> SumViewsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        var total = await db.PostViewDaily.AsNoTracking()
            .Where(v => v.ViewDateUtc >= startUtc && v.ViewDateUtc < endUtc)
            .Select(v => (int?)v.ViewCount)
            .SumAsync(ct);

        return total ?? 0;
    }

    private static int GetDaysSinceMonday(DateTime date) => ((int)date.DayOfWeek + 6) % 7;
}
