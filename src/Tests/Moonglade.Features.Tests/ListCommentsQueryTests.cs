using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Comment;

namespace Moonglade.Features.Tests;

public class ListCommentsQueryTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_DefaultSort_ReturnsNewestCommentsFirst()
    {
        using var db = CreateDbContext();
        await SeedCommentsAsync(db);
        var handler = new ListCommentsQueryHandler(db);

        var result = await handler.HandleAsync(new ListCommentsQuery(10, 1, new CommentFilter()), TestContext.Current.CancellationToken);

        Assert.Collection(result,
            c => Assert.Equal("Newest", c.Username),
            c => Assert.Equal("Oldest", c.Username));
    }

    [Fact]
    public async Task HandleAsync_SortAscending_ReturnsOldestCommentsFirst()
    {
        using var db = CreateDbContext();
        await SeedCommentsAsync(db);
        var handler = new ListCommentsQueryHandler(db);

        var filter = new CommentFilter(SortBy: "createTimeUtc", SortDescending: false);
        var result = await handler.HandleAsync(new ListCommentsQuery(10, 1, filter), TestContext.Current.CancellationToken);

        Assert.Collection(result,
            c => Assert.Equal("Oldest", c.Username),
            c => Assert.Equal("Newest", c.Username));
    }

    private static async Task SeedCommentsAsync(BlogDbContext db)
    {
        var postId = Guid.NewGuid();
        db.Post.Add(new PostEntity
        {
            Id = postId,
            Title = "Test Post"
        });

        db.Comment.AddRange(
            new CommentEntity
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                Username = "Oldest",
                Email = "oldest@example.com",
                IPAddress = "127.0.0.1",
                CommentContent = "Oldest comment",
                CreateTimeUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CommentEntity
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                Username = "Newest",
                Email = "newest@example.com",
                IPAddress = "127.0.0.1",
                CommentContent = "Newest comment",
                CreateTimeUtc = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
