using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Tag;
using Moq;

namespace Moonglade.Features.Tests;

public class TagCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task CreateTagCommand_CreatesTagWithNormalizedName()
    {
        using var db = CreateDbContext();
        var handler = new CreateTagCommandHandler(db, Mock.Of<ILogger<CreateTagCommandHandler>>());

        var result = await handler.HandleAsync(new CreateTagCommand("ASP.NET Core"), TestContext.Current.CancellationToken);

        var tag = await db.Tag.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("ASP.NET Core", result.DisplayName);
        Assert.Equal(tag.Id, result.Id);
        Assert.Equal(tag.NormalizedName, result.NormalizedName);
        Assert.NotEqual("ASP.NET Core", result.NormalizedName);
    }

    [Fact]
    public async Task CreateTagCommand_ExistingNormalizedName_ReturnsExistingTagWithoutCreatingDuplicate()
    {
        using var db = CreateDbContext();
        var handler = new CreateTagCommandHandler(db, Mock.Of<ILogger<CreateTagCommandHandler>>());
        var first = await handler.HandleAsync(new CreateTagCommand("ASP.NET Core"), TestContext.Current.CancellationToken);

        var second = await handler.HandleAsync(new CreateTagCommand("asp-net-core"), TestContext.Current.CancellationToken);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.NormalizedName, second.NormalizedName);
        Assert.Single(await db.Tag.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateTagCommand_ExistingTag_UpdatesDisplayAndNormalizedName()
    {
        using var db = CreateDbContext();
        var tag = new TagEntity { DisplayName = "Old", NormalizedName = "old" };
        db.Tag.Add(tag);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateTagCommandHandler(db, Mock.Of<ILogger<UpdateTagCommandHandler>>());

        var result = await handler.HandleAsync(new UpdateTagCommand(tag.Id, "New Tag"), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        var savedTag = await db.Tag.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("New Tag", savedTag.DisplayName);
        Assert.NotEqual("old", savedTag.NormalizedName);
    }

    [Fact]
    public async Task UpdateTagCommand_MissingTag_ReturnsObjectNotFound()
    {
        using var db = CreateDbContext();
        var handler = new UpdateTagCommandHandler(db, Mock.Of<ILogger<UpdateTagCommandHandler>>());

        var result = await handler.HandleAsync(new UpdateTagCommand(42, "Missing"), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.ObjectNotFound, result);
    }

    [Fact]
    public async Task DeleteTagCommand_ExistingTag_RemovesTagAndPostTagLinks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new BlogDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var post = CreatePostEntity(Guid.NewGuid());
        var tag = new TagEntity { DisplayName = "Delete Me", NormalizedName = "delete-me" };
        post.Tags.Add(tag);
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteTagCommandHandler(db, Mock.Of<ILogger<DeleteTagCommandHandler>>());

        var result = await handler.HandleAsync(new DeleteTagCommand(tag.Id), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        Assert.Empty(await db.Tag.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await db.PostTag.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await db.Post.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteTagCommand_MissingTag_ReturnsObjectNotFound()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new BlogDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteTagCommandHandler(db, Mock.Of<ILogger<DeleteTagCommandHandler>>());

        var result = await handler.HandleAsync(new DeleteTagCommand(42), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.ObjectNotFound, result);
    }

    private static PostEntity CreatePostEntity(Guid id)
    {
        return new PostEntity
        {
            Id = id,
            Title = "Test Post",
            Slug = "test-post",
            Author = "Author",
            PostContent = "Content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow.AddDays(-1),
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1),
            ContentAbstract = "Abstract",
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow.AddDays(-1),
            PostStatus = PostStatus.Published,
            IsDeleted = false,
            RouteLink = "2024/1/1/test-post",
            ContentType = "html"
        };
    }
}
