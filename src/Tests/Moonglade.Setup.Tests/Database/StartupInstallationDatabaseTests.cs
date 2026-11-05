using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Setup.Tests.TestSupport;
using System.Data.Common;

namespace Moonglade.Setup.Tests.Database;

[Collection("Startup site icon cache")]
public class StartupInstallationDatabaseTests(DisposableCategoryDatabases databases) : IClassFixture<DisposableCategoryDatabases>
{
    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", false)]
    [InlineData("SqlServer", false)]
    [InlineData("PostgreSql", true)]
    [InlineData("SqlServer", true)]
    public async Task FreshInstallationAndRestarts_AreIdempotent(string provider, bool schemaExists)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = CreateContext(database);
        if (schemaExists) await db.Database.EnsureCreatedAsync(ct);
        using var harness = new StartupTestHarness(db);

        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));

        var postId = (await db.Post.AsNoTracking().SingleAsync(ct)).Id;
        var manifest = harness.Config.SystemManifestSettings.ToJson();
        var original = await db.BlogConfiguration.AsNoTracking().OrderBy(row => row.CfgKey).ToListAsync(ct);
        for (var restart = 0; restart < 2; restart++)
        {
            await using var restartedDb = CreateContext(database);
            using var restarted = new StartupTestHarness(restartedDb);
            Assert.Equal(InitStartUpResult.Success, await restarted.Initializer.InitStartUpAsync(ct));
            Assert.Equal(postId, (await restartedDb.Post.SingleAsync(ct)).Id);
            Assert.Equal(manifest, restarted.Config.SystemManifestSettings.ToJson());
            var saved = await restartedDb.BlogConfiguration.OrderBy(row => row.CfgKey).ToListAsync(ct);
            Assert.Equal(original.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)),
                saved.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)));
            await AssertOneInstallationAsync(restartedDb, ct);
        }
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", 0, false)]
    [InlineData("SqlServer", 0, false)]
    [InlineData("PostgreSql", 1, false)]
    [InlineData("SqlServer", 1, false)]
    [InlineData("PostgreSql", 6, false)]
    [InlineData("SqlServer", 6, false)]
    [InlineData("PostgreSql", 11, false)]
    [InlineData("SqlServer", 11, false)]
    [InlineData("PostgreSql", 6, true)]
    [InlineData("SqlServer", 6, true)]
    public async Task FailedInstallation_RollsBackAndCanRestart(string provider, int settingNumber, bool cancelled)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        var failure = new InstallationWriteFailure(settingNumber, cancelled
            ? new OperationCanceledException() : new InvalidOperationException("Injected installation write failure."));
        await using (var failedDb = CreateContext(database, failure))
        {
            using var failed = new StartupTestHarness(failedDb);
            var originalRuntime = failed.Config.GeneralSettings;
            Assert.Equal(cancelled ? InitStartUpResult.FailedCancellation : InitStartUpResult.FailedSeedingDatabase,
                await failed.Initializer.InitStartUpAsync(ct));
            Assert.True(failure.Triggered);
            Assert.Same(originalRuntime, failed.Config.GeneralSettings);
            Assert.Null(failed.Config.LocalAccountSettings.Username);
        }
        await using var restartedDb = CreateContext(database);
        Assert.False(await restartedDb.Post.AnyAsync(ct));
        Assert.False(await restartedDb.Category.AnyAsync(ct));
        Assert.False(await restartedDb.Tag.AnyAsync(ct));
        Assert.False(await restartedDb.Widget.AnyAsync(ct));
        Assert.False(await restartedDb.BlogPage.AnyAsync(ct));
        Assert.False(await restartedDb.BlogConfiguration.AnyAsync(ct));
        using var restarted = new StartupTestHarness(restartedDb);

        Assert.Equal(InitStartUpResult.Success, await restarted.Initializer.InitStartUpAsync(ct));
        await AssertOneInstallationAsync(restartedDb, ct);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task TransientFailure_RetriesWholeInstallation(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        var failure = new InstallationWriteFailure(6, new TimeoutException("Injected transient installation failure."));
        await using var db = CreateContext(database, failure);
        using var harness = new StartupTestHarness(db);

        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));

        Assert.True(failure.Triggered);
        await AssertOneInstallationAsync(db, ct);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task LostCommitAcknowledgment_VerifiesCommittedDataWithoutReseeding(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        var failure = new LostCommitAcknowledgment();
        await using var db = CreateContext(database, failure);
        using var harness = new StartupTestHarness(db);
        var originalRuntime = harness.Config.GeneralSettings;
        failure.BeforeThrow = () => Assert.Same(originalRuntime, harness.Config.GeneralSettings);

        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));

        Assert.True(failure.Triggered);
        Assert.NotSame(originalRuntime, harness.Config.GeneralSettings);
        await AssertOneInstallationAsync(db, ct);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", false)]
    [InlineData("SqlServer", false)]
    [InlineData("PostgreSql", true)]
    [InlineData("SqlServer", true)]
    public async Task ExistingContent_WithMissingSettingsOrAccount_IsNotModified(string provider, bool accountOnly)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = CreateContext(database);
        using var harness = new StartupTestHarness(db);
        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
        var post = await db.Post.SingleAsync(ct);
        var comment = new CommentEntity
        {
            Id = Guid.NewGuid(), PostId = post.Id, Username = "Preserve", Email = "preserve@example.com",
            IPAddress = "127.0.0.1", CommentContent = "Preserve this comment", CreateTimeUtc = DateTime.UtcNow
        };
        db.Comment.Add(comment);
        await db.SaveChangesAsync(ct);
        post = await db.Post.AsNoTracking().SingleAsync(ct);
        if (accountOnly)
            await db.BlogConfiguration.Where(row => row.CfgKey == nameof(LocalAccountSettings)).ExecuteDeleteAsync(ct);
        else
            await db.BlogConfiguration.ExecuteDeleteAsync(ct);
        db.ChangeTracker.Clear();
        using var restarted = new StartupTestHarness(db);

        Assert.Equal(InitStartUpResult.FailedInitBlogConfig, await restarted.Initializer.InitStartUpAsync(ct));

        Assert.Equal(accountOnly ? 10 : 0, await db.BlogConfiguration.CountAsync(ct));
        var savedPost = await db.Post.AsNoTracking().SingleAsync(ct);
        Assert.Equal(post.Id, savedPost.Id);
        Assert.Equal(post.PostContent, savedPost.PostContent);
        Assert.Equal(post.CreateTimeUtc, savedPost.CreateTimeUtc);
        var savedComment = await db.Comment.AsNoTracking().SingleAsync(ct);
        Assert.Equal(comment.Id, savedComment.Id);
        Assert.Equal(post.Id, savedComment.PostId);
        Assert.Equal(comment.CommentContent, savedComment.CommentContent);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task FailedEmptinessQuery_DoesNotAssumeNewDatabase(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using (var schema = CreateContext(database)) await schema.Database.EnsureCreatedAsync(ct);
        var failure = new ApplicationQueryFailure();
        await using var db = CreateContext(database, failure);
        using var harness = new StartupTestHarness(db);

        Assert.Equal(InitStartUpResult.UnexpectedError, await harness.Initializer.InitStartUpAsync(ct));

        Assert.True(failure.Triggered);
        Assert.False(await db.Post.AnyAsync(ct));
        Assert.False(await db.BlogConfiguration.AnyAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task FailedSchemaCreation_CanBeRetriedAfterRecovery(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        var failure = new SchemaCreationFailure();
        await using var db = CreateContext(database, failure);
        using var harness = new StartupTestHarness(db);

        Assert.Equal(InitStartUpResult.FailedCreateDatabase, await harness.Initializer.InitStartUpAsync(ct));

        Assert.True(failure.Triggered);
        Assert.Null(harness.Config.LocalAccountSettings.Username);
        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
        await AssertOneInstallationAsync(db, ct);
    }

    private static BlogDbContext CreateContext(CategoryTestDatabase database, params IInterceptor[] interceptors) =>
        database.Provider == "PostgreSql"
            ? new PostgreSqlBlogDbContext(new DbContextOptionsBuilder<PostgreSqlBlogDbContext>()
                .UseNpgsql(database.Connection, options => options.EnableRetryOnFailure(1, TimeSpan.Zero, null))
                .AddInterceptors(interceptors).Options)
            : new SqlServerBlogDbContext(new DbContextOptionsBuilder<SqlServerBlogDbContext>()
                .UseSqlServer(database.Connection, options => options.EnableRetryOnFailure(1, TimeSpan.Zero, null))
                .AddInterceptors(interceptors).Options);

    private static async Task AssertOneInstallationAsync(BlogDbContext db, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        Assert.Single(await db.Post.ToListAsync(ct));
        Assert.Single(await db.Category.ToListAsync(ct));
        Assert.Equal(2, await db.Tag.CountAsync(ct));
        Assert.Equal(11, await db.BlogConfiguration.CountAsync(ct));
        Assert.False(string.IsNullOrWhiteSpace((await db.BlogConfiguration.SingleAsync(
            row => row.CfgKey == nameof(LocalAccountSettings), ct)).CfgValue.FromJson<LocalAccountSettings>().Username));
    }

    private sealed class InstallationWriteFailure(int settingNumber, Exception failure) : SaveChangesInterceptor
    {
        public bool Triggered { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!Triggered && eventData.Context!.ChangeTracker.Entries<BlogConfigurationEntity>().Count() == settingNumber)
            {
                Triggered = true;
                throw failure;
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class LostCommitAcknowledgment : DbTransactionInterceptor
    {
        public bool Triggered { get; private set; }
        public Action BeforeThrow { get; set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (!Triggered && eventData.Context!.ChangeTracker.Entries<BlogConfigurationEntity>().Count() == 11)
            {
                Triggered = true;
                BeforeThrow();
                throw new TimeoutException("Injected lost installation commit acknowledgment.");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class ApplicationQueryFailure : DbCommandInterceptor
    {
        public bool Triggered { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (!Triggered && (command.CommandText.Contains("blog_configuration") || command.CommandText.Contains("[BlogConfiguration]")))
            {
                Triggered = true;
                throw new InvalidOperationException("Injected application-state query failure.");
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SchemaCreationFailure : DbCommandInterceptor
    {
        public bool Triggered { get; private set; }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!Triggered && command.CommandText.Contains("CREATE TABLE"))
            {
                Triggered = true;
                throw new InvalidOperationException("Injected schema dependency failure.");
            }
            return ValueTask.FromResult(result);
        }
    }
}
