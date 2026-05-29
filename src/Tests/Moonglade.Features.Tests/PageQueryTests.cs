using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Page;

namespace Moonglade.Features.Tests;

public class PageQueryTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task GetPageBySlugQuery_ReturnsOnlyNotDeletedPage()
    {
        using var db = CreateDbContext();
        db.BlogPage.AddRange(
            CreatePageEntity(Guid.NewGuid(), "active-page"),
            CreatePageEntity(Guid.NewGuid(), "deleted-page", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPageBySlugQueryHandler(db);

        var active = await handler.HandleAsync(new GetPageBySlugQuery("active-page"), TestContext.Current.CancellationToken);
        var deleted = await handler.HandleAsync(new GetPageBySlugQuery("deleted-page"), TestContext.Current.CancellationToken);

        Assert.NotNull(active);
        Assert.Equal("active-page", active.Slug);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ListPageSegmentsQuery_DefaultReturnsOnlyNotDeletedPages()
    {
        using var db = CreateDbContext();
        db.BlogPage.AddRange(
            CreatePageEntity(Guid.NewGuid(), "active-page"),
            CreatePageEntity(Guid.NewGuid(), "deleted-page", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListPageSegmentsQueryHandler(db);

        var result = await handler.HandleAsync(new ListPageSegmentsQuery(), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("active-page", result[0].Slug);
        Assert.False(result[0].IsDeleted);
    }

    [Fact]
    public async Task ListPageSegmentsQuery_DeletedOnlyReturnsDeletedPages()
    {
        using var db = CreateDbContext();
        db.BlogPage.AddRange(
            CreatePageEntity(Guid.NewGuid(), "active-page"),
            CreatePageEntity(Guid.NewGuid(), "deleted-page", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListPageSegmentsQueryHandler(db);

        var result = await handler.HandleAsync(new ListPageSegmentsQuery(DeletedOnly: true), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("deleted-page", result[0].Slug);
        Assert.True(result[0].IsDeleted);
    }

    private static PageEntity CreatePageEntity(Guid id, string slug, bool isDeleted = false)
    {
        return new PageEntity
        {
            Id = id,
            Title = slug,
            Slug = slug,
            MetaDescription = "Meta description",
            HtmlContent = "<p>Hello</p>",
            CssId = string.Empty,
            HideSidebar = false,
            IsPublished = true,
            IsDeleted = isDeleted,
            CreateTimeUtc = DateTime.UtcNow.AddDays(-1),
            UpdateTimeUtc = DateTime.UtcNow.AddDays(-1)
        };
    }
}
