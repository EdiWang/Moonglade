using LiteBus.Events.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Features.Tag;
using Moq;
using System.Data.Common;

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
        var handler = new UpdateTagCommandHandler(db, Mock.Of<ILogger<UpdateTagCommandHandler>>(), Mock.Of<IEventMediator>());

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
        var handler = new UpdateTagCommandHandler(db, Mock.Of<ILogger<UpdateTagCommandHandler>>(), Mock.Of<IEventMediator>());

        var result = await handler.HandleAsync(new UpdateTagCommand(42, "Missing"), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.ObjectNotFound, result);
    }

    [Fact]
    public async Task DeleteTagCommand_ExistingTag_RemovesTagAndPostTagLinks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = CreateRelationalDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var post = CreatePostEntity(Guid.NewGuid());
        var tag = new TagEntity { DisplayName = "Delete Me", NormalizedName = "delete-me" };
        post.Tags.Add(tag);
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var events = new Mock<IEventMediator>(MockBehavior.Strict);
        events.Setup(m => m.PublishAsync(It.IsAny<PostChangedEvent>(), default, CancellationToken.None))
            .Callback(() => Assert.Null(db.Database.CurrentTransaction))
            .Returns(Task.CompletedTask);
        var handler = new DeleteTagCommandHandler(db, Mock.Of<ILogger<DeleteTagCommandHandler>>(), events.Object);

        var result = await handler.HandleAsync(new DeleteTagCommand(tag.Id), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        Assert.Empty(await db.Tag.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await db.PostTag.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await db.Post.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(events.Invocations);
    }

    [Fact]
    public async Task DeleteTagCommand_MissingTag_ReturnsObjectNotFound()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = CreateRelationalDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var events = new Mock<IEventMediator>();
        var handler = new DeleteTagCommandHandler(db, Mock.Of<ILogger<DeleteTagCommandHandler>>(), events.Object);

        var result = await handler.HandleAsync(new DeleteTagCommand(42), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.ObjectNotFound, result);
        Assert.Empty(events.Invocations);
    }

    [Fact]
    public async Task DeleteTagCommand_DeleteFails_RollsBackLinksWithoutPublishing()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = CreateRelationalDbContext(connection);
        await db.Database.EnsureCreatedAsync(ct);
        var post = CreatePostEntity(Guid.NewGuid());
        var tag = new TagEntity { DisplayName = "Protected", NormalizedName = "protected" };
        post.Tags.Add(tag);
        db.Post.Add(post);
        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_tag_delete BEFORE DELETE ON Tag BEGIN SELECT RAISE(ABORT, 'Delete rejected'); END;", ct);
        var events = new Mock<IEventMediator>();
        var handler = new DeleteTagCommandHandler(db, Mock.Of<ILogger<DeleteTagCommandHandler>>(), events.Object);

        await Assert.ThrowsAsync<SqliteException>(() => handler.HandleAsync(new DeleteTagCommand(tag.Id), ct));

        Assert.Single(await db.Tag.ToListAsync(ct));
        Assert.Single(await db.PostTag.ToListAsync(ct));
        Assert.Single(await db.Post.ToListAsync(ct));
        Assert.Empty(events.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteTagCommand_TransientFailure_DeletesAtomicallyAndPublishesOnce(bool failAfterCommit)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var statementFailure = new FailFirstTagDeleteInterceptor();
        var commitFailure = new FailAfterCommitInterceptor();
        await using var db = CreateRelationalDbContext(connection,
            failAfterCommit ? commitFailure : statementFailure);
        await db.Database.EnsureCreatedAsync(ct);
        var post = CreatePostEntity(Guid.NewGuid());
        var tag = new TagEntity { DisplayName = "Retry", NormalizedName = "retry" };
        post.Tags.Add(tag);
        db.Post.Add(post);
        await db.SaveChangesAsync(ct);
        commitFailure.Enabled = true;
        var events = new Mock<IEventMediator>();
        var handler = new DeleteTagCommandHandler(db, Mock.Of<ILogger<DeleteTagCommandHandler>>(), events.Object);

        var result = await handler.HandleAsync(new DeleteTagCommand(tag.Id), ct);

        Assert.Equal(OperationCode.Done, result);
        Assert.Empty(await db.Tag.ToListAsync(ct));
        Assert.Empty(await db.PostTag.ToListAsync(ct));
        Assert.Single(await db.Post.ToListAsync(ct));
        Assert.Single(events.Invocations);
        events.Verify(m => m.PublishAsync(It.IsAny<PostChangedEvent>(), default, CancellationToken.None), Times.Once);
        if (failAfterCommit)
        {
            Assert.Equal(1, commitFailure.Commits);
        }
        else
        {
            Assert.Equal(2, statementFailure.Attempts);
            Assert.Equal([1, 1], statementFailure.DeletedLinks);
        }
    }

    private static BlogDbContext CreateRelationalDbContext(SqliteConnection connection, params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection, sqlite => sqlite.ExecutionStrategy(dependencies => new TestRetryingExecutionStrategy(dependencies)))
            .AddInterceptors(interceptors)
            .Options;
        return new BlogDbContext(options);
    }

    private sealed class TestRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TimeoutException;
    }

    private sealed class FailFirstTagDeleteInterceptor : DbCommandInterceptor
    {
        public int Attempts { get; private set; }
        public List<int> DeletedLinks { get; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("DELETE FROM \"Tag\"", StringComparison.Ordinal) && ++Attempts == 1)
            {
                throw new TimeoutException("Transient tag delete failure");
            }
            return ValueTask.FromResult(result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("DELETE FROM \"PostTag\"", StringComparison.Ordinal)) DeletedLinks.Add(result);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FailAfterCommitInterceptor : DbTransactionInterceptor
    {
        public bool Enabled { get; set; }
        public int Commits { get; private set; }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (Enabled && ++Commits == 1) throw new TimeoutException("Commit acknowledgement lost");
            return Task.CompletedTask;
        }
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
