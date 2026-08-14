using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moq;
using System.Data.Common;
using System.Reflection;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace Moonglade.Setup.Tests;

[CollectionDefinition(Name)]
public sealed class RelationalMigrationCollection : ICollectionFixture<RelationalDatabaseFixture>
{
    public const string Name = "Relational migration containers";
}

public sealed class RelationalDatabaseFixture : IAsyncLifetime
{
    public MsSqlContainer SqlServer { get; } = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();

    public PostgreSqlContainer PostgreSql { get; } = new PostgreSqlBuilder("postgres:17.6-alpine")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(SqlServer.StartAsync(), PostgreSql.StartAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(SqlServer.DisposeAsync().AsTask(), PostgreSql.DisposeAsync().AsTask());
    }
}

[Collection(RelationalMigrationCollection.Name)]
public sealed class RelationalMigrationHarnessTests(RelationalDatabaseFixture fixture)
{
    private const string SeedPostId = "11111111-1111-1111-1111-111111111111";

    [Fact]
    [Trait("Category", "Docker")]
    public async Task SqlServer_CumulativeMigration_IsTransactionalAndIdempotent()
    {
        await using var context = CreateSqlServerContext(fixture.SqlServer.GetConnectionString());

        await VerifyMigrationHarnessAsync(
            context,
            providerKey: "SqlServer",
            fixtureResourceName: "MigrationFixtures.SqlServer.latest-stable.sql",
            baselineTemporalColumnCount: 22,
            migratedTemporalColumnCount: 24,
            temporalColumnCountSql: """
                SELECT COUNT(*)
                FROM sys.columns AS c
                INNER JOIN sys.types AS ty ON c.user_type_id = ty.user_type_id
                INNER JOIN sys.tables AS t ON c.object_id = t.object_id
                INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
                WHERE s.name = 'dbo' AND ty.name = 'datetime' AND t.is_ms_shipped = 0;
                """,
            postCountSql: $"SELECT COUNT(*) FROM [dbo].[Post] WHERE [Id] = '{SeedPostId}';",
            requiredIndexCountSql: """
                SELECT COUNT(*)
                FROM sys.indexes
                WHERE name IN (
                    'IX_EmailOutboxMessage_Dequeue',
                    'IX_PostViewDaily_ViewDateUtc',
                    'IX_SiteVerificationFile_NormalizedFileName'
                );
                """,
            baselineIndexCount: 2,
            migratedIndexCount: 3,
            rollbackScript: """
                CREATE TABLE [dbo].[MigrationRollbackProbe]([Id] [int] NOT NULL);
                GO
                SELECT * FROM [dbo].[MissingMigrationTable];
                """,
            rollbackProbeCountSql: """
                SELECT COUNT(*)
                FROM sys.tables
                WHERE object_id = OBJECT_ID(N'[dbo].[MigrationRollbackProbe]');
                """,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task PostgreSql_CumulativeMigration_IsTransactionalAndIdempotent()
    {
        await using var context = CreatePostgreSqlContext(fixture.PostgreSql.GetConnectionString());

        await VerifyMigrationHarnessAsync(
            context,
            providerKey: "PostgreSql",
            fixtureResourceName: "MigrationFixtures.PostgreSql.latest-stable.sql",
            baselineTemporalColumnCount: 22,
            migratedTemporalColumnCount: 24,
            temporalColumnCountSql: """
                SELECT COUNT(*)
                FROM information_schema.columns
                WHERE table_schema = 'public' AND data_type = 'timestamp without time zone';
                """,
            postCountSql: $"SELECT COUNT(*) FROM \"Post\" WHERE \"Id\" = '{SeedPostId}';",
            requiredIndexCountSql: """
                SELECT COUNT(*)
                FROM pg_indexes
                WHERE schemaname = 'public'
                  AND indexname IN (
                      'IX_EmailOutboxMessage_Dequeue',
                      'IX_PostViewDaily_ViewDateUtc',
                      'IX_SiteVerificationFile_NormalizedFileName'
                  );
                """,
            baselineIndexCount: 2,
            migratedIndexCount: 3,
            rollbackScript: """
                CREATE TABLE "MigrationRollbackProbe" ("Id" INTEGER NOT NULL);
                GO
                SELECT * FROM "MissingMigrationTable";
                """,
            rollbackProbeCountSql: """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'MigrationRollbackProbe';
                """,
            TestContext.Current.CancellationToken);
    }

    private static async Task VerifyMigrationHarnessAsync(
        BlogDbContext context,
        string providerKey,
        string fixtureResourceName,
        int baselineTemporalColumnCount,
        int migratedTemporalColumnCount,
        string temporalColumnCountSql,
        string postCountSql,
        string requiredIndexCountSql,
        int baselineIndexCount,
        int migratedIndexCount,
        string rollbackScript,
        string rollbackProbeCountSql,
        CancellationToken cancellationToken)
    {
        var manager = CreateManager();

        await manager.ExecuteMigrationScriptBatchesAsync(
            LoadFixture(fixtureResourceName), context, cancellationToken);

        Assert.Equal(baselineTemporalColumnCount, await ExecuteScalarIntAsync(context, temporalColumnCountSql, cancellationToken));
        Assert.Equal(1, await ExecuteScalarIntAsync(context, postCountSql, cancellationToken));
        Assert.Equal(baselineIndexCount, await ExecuteScalarIntAsync(context, requiredIndexCountSql, cancellationToken));

        var cumulativeScript = manager.LoadEmbeddedMigrationScript(providerKey);
        Assert.False(string.IsNullOrWhiteSpace(cumulativeScript));

        await manager.ExecuteMigrationScriptBatchesAsync(cumulativeScript, context, cancellationToken);
        await manager.ExecuteMigrationScriptBatchesAsync(cumulativeScript, context, cancellationToken);

        Assert.Equal(migratedTemporalColumnCount, await ExecuteScalarIntAsync(context, temporalColumnCountSql, cancellationToken));
        Assert.Equal(1, await ExecuteScalarIntAsync(context, postCountSql, cancellationToken));
        Assert.Equal(migratedIndexCount, await ExecuteScalarIntAsync(context, requiredIndexCountSql, cancellationToken));

        await VerifyStartupOrderingAsync(context, cancellationToken);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            manager.ExecuteMigrationScriptBatchesAsync(rollbackScript, context, cancellationToken));

        Assert.Equal(0, await ExecuteScalarIntAsync(context, rollbackProbeCountSql, cancellationToken));
    }

    private static MigrationManager CreateManager()
    {
        return new MigrationManager(
            NullLogger<MigrationManager>.Instance,
            Mock.Of<ICommandMediator>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IBlogConfig>());
    }

    private static SqlServerBlogDbContext CreateSqlServerContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SqlServerBlogDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SqlServerBlogDbContext(options);
    }

    private static PostgreSqlBlogDbContext CreatePostgreSqlContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PostgreSqlBlogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PostgreSqlBlogDbContext(options);
    }

    private static async Task VerifyStartupOrderingAsync(
        BlogDbContext context,
        CancellationToken cancellationToken)
    {
        var operations = new List<string>();
        var configInitializer = new Mock<IConfigInitializer>(MockBehavior.Strict);
        configInitializer
            .Setup(x => x.Load(cancellationToken))
            .Callback(() => operations.Add("load configuration"))
            .Returns(Task.CompletedTask);
        configInitializer
            .Setup(x => x.Initialize(false, cancellationToken))
            .Callback(() => operations.Add("initialize configuration"))
            .Returns(Task.CompletedTask);

        var migrationManager = new Mock<IMigrationManager>(MockBehavior.Strict);
        migrationManager
            .Setup(x => x.TryMigrationAsync(context, cancellationToken))
            .Callback(() => operations.Add("migrate database"))
            .ReturnsAsync(new MigrationResult(MigrationStatus.NotRequired));

        var siteIconBuilder = new Mock<ISiteIconBuilder>(MockBehavior.Strict);
        siteIconBuilder
            .Setup(x => x.GenerateSiteIcons())
            .Returns(Task.CompletedTask);

        var initializer = new StartUpInitializer(
            NullLogger<StartUpInitializer>.Instance,
            context,
            configInitializer.Object,
            migrationManager.Object,
            new ConfigurationBuilder().Build(),
            siteIconBuilder.Object);

        var result = await initializer.InitStartUpAsync(cancellationToken);

        Assert.Equal(InitStartUpResult.Success, result);
        Assert.Equal<string>(
            ["load configuration", "migrate database", "initialize configuration"],
            operations);
    }

    private static string LoadFixture(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded fixture '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task<int> ExecuteScalarIntAsync(
        DbContext context,
        string commandText,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result);
    }
}
