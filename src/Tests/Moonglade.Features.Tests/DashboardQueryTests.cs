using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Dashboard;
using Moonglade.Features.Post;

namespace Moonglade.Features.Tests;

public class DashboardQueryTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task GetDashboardStatsQuery_ReturnsViewPostAndTaxonomyCounts()
    {
        using var db = CreateDbContext();
        var now = new DateTime(2024, 4, 17, 8, 0, 0, DateTimeKind.Utc);

        db.PostViewDaily.AddRange(
            CreatePostViewDaily(now.Date.AddDays(-1), 3),
            CreatePostViewDaily(now.Date.AddDays(-2), 5),
            CreatePostViewDaily(new DateTime(2024, 4, 1), 7),
            CreatePostViewDaily(new DateTime(2024, 3, 31), 11));

        db.Post.AddRange(
            CreatePost(PostStatus.Published),
            CreatePost(PostStatus.Published),
            CreatePost(PostStatus.Draft),
            CreatePost(PostStatus.Scheduled),
            CreatePost(PostStatus.Published, isDeleted: true));

        db.Category.AddRange(
            new CategoryEntity { Id = Guid.NewGuid(), DisplayName = "Cloud", Slug = "cloud" },
            new CategoryEntity { Id = Guid.NewGuid(), DisplayName = "Dev", Slug = "dev" });
        db.Tag.Add(new TagEntity { DisplayName = "Azure", NormalizedName = "azure" });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetDashboardStatsQueryHandler(db);

        var stats = await handler.HandleAsync(new GetDashboardStatsQuery(now), TestContext.Current.CancellationToken);

        Assert.Equal(3, stats.YesterdayViews);
        Assert.Equal(8, stats.ThisWeekViews);
        Assert.Equal(15, stats.ThisMonthViews);
        Assert.Equal(2, stats.PublishedPostCount);
        Assert.Equal(1, stats.DraftPostCount);
        Assert.Equal(1, stats.ScheduledPostCount);
        Assert.Equal(2, stats.CategoryCount);
        Assert.Equal(1, stats.TagCount);
    }

    [Fact]
    public async Task GetDashboardStatsQuery_IncludesPreviousMonthViewsInCurrentWeek()
    {
        using var db = CreateDbContext();
        var now = new DateTime(2024, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        db.PostViewDaily.AddRange(
            CreatePostViewDaily(new DateTime(2024, 4, 29), 5),
            CreatePostViewDaily(new DateTime(2024, 4, 30), 3),
            CreatePostViewDaily(new DateTime(2024, 5, 1), 7),
            CreatePostViewDaily(new DateTime(2024, 4, 28), 11));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetDashboardStatsQueryHandler(db);

        var stats = await handler.HandleAsync(new GetDashboardStatsQuery(now), TestContext.Current.CancellationToken);

        Assert.Equal(3, stats.YesterdayViews);
        Assert.Equal(15, stats.ThisWeekViews);
        Assert.Equal(7, stats.ThisMonthViews);
    }

    private static PostViewDailyEntity CreatePostViewDaily(DateTime dateUtc, int viewCount) =>
        new()
        {
            PostId = Guid.NewGuid(),
            ViewDateUtc = dateUtc,
            ViewCount = viewCount
        };

    private static PostEntity CreatePost(PostStatus status, bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "Test Post",
            Slug = Guid.NewGuid().ToString("N"),
            Author = "Author",
            PostContent = "Content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow.AddDays(-1),
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1),
            ContentAbstract = "Abstract",
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = status == PostStatus.Published ? DateTime.UtcNow.AddDays(-1) : null,
            PostStatus = status,
            IsDeleted = isDeleted,
            RouteLink = status == PostStatus.Published ? "2024/4/17/test-post" : null,
            ContentType = "html"
        };
}
