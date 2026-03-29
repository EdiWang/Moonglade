using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Webmention.Tests;

public class DeleteMentionsCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_NullIds_ReturnsWithoutDeleting()
    {
        using var db = CreateDbContext();
        db.Mention.Add(new MentionEntity
        {
            Id = Guid.NewGuid(),
            Domain = "test.com",
            SourceUrl = "https://test.com",
            SourceTitle = "Test",
            SourceIp = "1.2.3.4",
            TargetPostId = Guid.NewGuid(),
            PingTimeUtc = DateTime.UtcNow,
            TargetPostTitle = "Post"
        });
        await db.SaveChangesAsync();

        var handler = new DeleteMentionsCommandHandler(db);
        var command = new DeleteMentionsCommand(null!);

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(1, await db.Mention.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_EmptyIds_ReturnsWithoutDeleting()
    {
        using var db = CreateDbContext();
        db.Mention.Add(new MentionEntity
        {
            Id = Guid.NewGuid(),
            Domain = "test.com",
            SourceUrl = "https://test.com",
            SourceTitle = "Test",
            SourceIp = "1.2.3.4",
            TargetPostId = Guid.NewGuid(),
            PingTimeUtc = DateTime.UtcNow,
            TargetPostTitle = "Post"
        });
        await db.SaveChangesAsync();

        var handler = new DeleteMentionsCommandHandler(db);
        var command = new DeleteMentionsCommand([]);

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(1, await db.Mention.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_NoMatchingEntities_DoesNotDelete()
    {
        using var db = CreateDbContext();
        db.Mention.Add(new MentionEntity
        {
            Id = Guid.NewGuid(),
            Domain = "test.com",
            SourceUrl = "https://test.com",
            SourceTitle = "Test",
            SourceIp = "1.2.3.4",
            TargetPostId = Guid.NewGuid(),
            PingTimeUtc = DateTime.UtcNow,
            TargetPostTitle = "Post"
        });
        await db.SaveChangesAsync();

        var handler = new DeleteMentionsCommandHandler(db);
        var command = new DeleteMentionsCommand([Guid.NewGuid()]);

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(1, await db.Mention.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_WithMatchingEntities_DeletesThem()
    {
        using var db = CreateDbContext();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        db.Mention.AddRange(
            new MentionEntity { Id = id1, Domain = "a.com", SourceUrl = "https://a.com", SourceTitle = "A", SourceIp = "1.1.1.1", TargetPostId = Guid.NewGuid(), PingTimeUtc = DateTime.UtcNow, TargetPostTitle = "P1" },
            new MentionEntity { Id = id2, Domain = "b.com", SourceUrl = "https://b.com", SourceTitle = "B", SourceIp = "2.2.2.2", TargetPostId = Guid.NewGuid(), PingTimeUtc = DateTime.UtcNow, TargetPostTitle = "P2" },
            new MentionEntity { Id = keepId, Domain = "c.com", SourceUrl = "https://c.com", SourceTitle = "C", SourceIp = "3.3.3.3", TargetPostId = Guid.NewGuid(), PingTimeUtc = DateTime.UtcNow, TargetPostTitle = "P3" }
        );
        await db.SaveChangesAsync();

        var handler = new DeleteMentionsCommandHandler(db);
        var command = new DeleteMentionsCommand([id1, id2]);

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(1, await db.Mention.CountAsync());
        Assert.NotNull(await db.Mention.FindAsync(keepId));
    }
}
