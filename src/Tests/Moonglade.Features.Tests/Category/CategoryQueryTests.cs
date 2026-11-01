using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;

namespace Moonglade.Features.Tests.Category;

public class CategoryQueryTests
{
    [Fact]
    public async Task CategoryQueries_EmptyDatabase_ReturnsNoCategoriesOrMatches()
    {
        using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Assert.Empty(await new ListCategoriesQueryHandler(db)
            .HandleAsync(new ListCategoriesQuery(), TestContext.Current.CancellationToken));
        Assert.Null(await new GetCategoryByIdQueryHandler(db)
            .HandleAsync(new GetCategoryQuery(100), TestContext.Current.CancellationToken));
        Assert.Null(await new GetCategoryByRouteQueryHandler(db)
            .HandleAsync(new GetCategoryBySlugQuery("default"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CategoryQueries_IntegerIdAndPublicSlug_SelectExactCategoryAndReturnNullOnMiss()
    {
        using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Category.AddRange(
            new CategoryEntity { Id = 100, DisplayName = "Cloud", Slug = "cloud", Note = "Cloud notes" },
            new CategoryEntity { Id = 101, DisplayName = "Other", Slug = "other", Note = "Other notes" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        var byId = new GetCategoryByIdQueryHandler(db);
        var bySlug = new GetCategoryByRouteQueryHandler(db);
        var category = await byId.HandleAsync(new GetCategoryQuery(100), TestContext.Current.CancellationToken);
        Assert.Equal(100, category.Id);
        Assert.Equal("cloud", category.Slug);
        Assert.Equal("Cloud notes", category.Note);
        Assert.Null(await byId.HandleAsync(new GetCategoryQuery(999), TestContext.Current.CancellationToken));
        Assert.Equal(101, (await bySlug.HandleAsync(new GetCategoryBySlugQuery("other"), TestContext.Current.CancellationToken)).Id);
        Assert.Null(await bySlug.HandleAsync(new GetCategoryBySlugQuery("missing"), TestContext.Current.CancellationToken));

        db.ChangeTracker.Clear();
        var categories = await new ListCategoriesQueryHandler(db)
            .HandleAsync(new ListCategoriesQuery(), TestContext.Current.CancellationToken);
        Assert.Equal([100, 101], categories.Select(category => category.Id).Order().ToArray());
        Assert.Equal(["cloud", "other"], categories.Select(category => category.Slug).Order().ToArray());
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
