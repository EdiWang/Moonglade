using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Setup.Tests.TestSupport;
using OtpNet;
using System.Data.Common;
using System.Text.Json;

namespace Moonglade.Setup.Tests.Database;

[Collection("Startup site icon cache")]
public partial class OwnerAccountDatabaseTests(DisposableCategoryDatabases databases) : IClassFixture<DisposableCategoryDatabases>
{
    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task AdditiveUpgrade_MatchesFreshSchema_RerunsPreserveAllExistingData(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        using (var fresh = new StartupTestHarness(db))
        {
            Assert.Equal(InitStartUpResult.Success, await fresh.Initializer.InitStartUpAsync(ct));
            Assert.False(await db.OwnerAccount.AnyAsync(ct));
            db.Comment.Add(new CommentEntity
            {
                Id = Guid.NewGuid(),
                PostId = (await db.Post.SingleAsync(ct)).Id,
                Username = "Retained visitor",
                Email = "visitor@example.test",
                IPAddress = "192.0.2.1",
                CommentContent = "Retained account upgrade comment",
                CreateTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await db.SaveChangesAsync(ct);
        }
        db.ChangeTracker.Clear();
        var freshSchema = await SchemaAsync(database);
        var before = await UnrelatedRowsAsync(db, database, includeOidc: false);
        await database.MigrateAsync("owner-account.sql");
        Assert.Equal(freshSchema, await SchemaAsync(database));

        await DropOidcSchemaAsync(database);
        await database.ExecuteAsync(provider == "PostgreSql"
            ? "DROP TABLE public.owner_account;" : "DROP TABLE dbo.OwnerAccount;");
        using (var missingSchema = new StartupTestHarness(db))
            Assert.Equal(InitStartUpResult.UnexpectedError, await missingSchema.Initializer.InitStartUpAsync(ct));
        Assert.Equal(before, await UnrelatedRowsAsync(db, database, includeOidc: false));

        for (var run = 0; run < 2; run++)
        {
            await database.MigrateAsync("owner-account.sql");
            await database.MigrateAsync("owner-oidc.sql");
            Assert.Equal(freshSchema, await SchemaAsync(database));
            Assert.Equal(before, await UnrelatedRowsAsync(db, database, includeOidc: false));
            using var upgraded = new StartupTestHarness(db);
            Assert.Equal(InitStartUpResult.Success, await upgraded.Initializer.InitStartUpAsync(ct));
            Assert.False(await db.OwnerAccount.AnyAsync(ct));
            Assert.Equal(before, await UnrelatedRowsAsync(db, database, includeOidc: false));
        }

        var store = CreateStore(db);
        Assert.Null(await store.AuthenticateLocalAsync("owner@example.test", "Synthetic123!", ct));
        var profile = await store.CreateProfileAsync("  Owner@example.test ", ct);
        await database.MigrateAsync("owner-account.sql");
        Assert.Equal(profile, await store.ReadAsync(ct));
        Assert.Null(await store.AuthenticateLocalAsync(profile.Email, "Synthetic123!", ct));
        var pending = await store.PrepareLocalSetupAsync(profile.Email, "Synthetic123!", ct);
        var initialized = await store.CompleteLocalSetupAsync(pending, null, ct);
        var credentials = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        await database.MigrateAsync("owner-account.sql");
        var retained = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal((credentials.Id, credentials.Email, credentials.NormalizedEmail, credentials.Revision,
                credentials.LocalInitializedUtc, credentials.PasswordHash, credentials.PasswordSalt, credentials.TotpSecret),
            (retained.Id, retained.Email, retained.NormalizedEmail, retained.Revision,
                retained.LocalInitializedUtc, retained.PasswordHash, retained.PasswordSalt, retained.TotpSecret));
        Assert.Equal(DateTimeKind.Utc, retained.LocalInitializedUtc!.Value.Kind);
        Assert.Equal(initialized, await store.AuthenticateLocalAsync("OWNER@example.TEST", "Synthetic123!", ct));
        Assert.Equal(before, await UnrelatedRowsAsync(db, database, includeOidc: false));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", "missing-index")]
    [InlineData("SqlServer", "missing-index")]
    [InlineData("PostgreSql", "wrong-column")]
    [InlineData("SqlServer", "wrong-column")]
    [InlineData("PostgreSql", "unexpected-column")]
    [InlineData("SqlServer", "unexpected-column")]
    public async Task AdditiveUpgrade_RejectsUnsupportedSchemaWithoutChangingExistingData(string provider, string fault)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);
        var store = CreateStore(db);
        var profile = await store.CreateProfileAsync("preserved@example.test", ct);
        db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "Unrelated", CfgValue = "{}" });
        await db.SaveChangesAsync(ct);
        var before = await UnrelatedRowsAsync(db, database);
        await database.ExecuteAsync((provider, fault) switch
        {
            ("PostgreSql", "missing-index") => """DROP INDEX public."IX_OwnerAccount_NormalizedEmail";""",
            ("SqlServer", "missing-index") => "DROP INDEX IX_OwnerAccount_NormalizedEmail ON dbo.OwnerAccount;",
            ("PostgreSql", "wrong-column") => "ALTER TABLE public.owner_account ALTER COLUMN email TYPE varchar(255);",
            ("SqlServer", "wrong-column") => "ALTER TABLE dbo.OwnerAccount ALTER COLUMN Email nvarchar(255) NOT NULL;",
            ("PostgreSql", _) => "ALTER TABLE public.owner_account ADD mystery integer;",
            _ => "ALTER TABLE dbo.OwnerAccount ADD Mystery int;"
        });

        var exception = await Assert.ThrowsAnyAsync<DbException>(() => database.MigrateAsync("owner-account.sql"));

        Assert.Contains("unsupported or incomplete schema", exception.Message);
        Assert.Equal(profile, await store.ReadAsync(ct));
        Assert.Equal(before, await UnrelatedRowsAsync(db, database));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", "second-owner")]
    [InlineData("SqlServer", "second-owner")]
    [InlineData("PostgreSql", "invalid-slot")]
    [InlineData("SqlServer", "invalid-slot")]
    [InlineData("PostgreSql", "normalized-email")]
    [InlineData("SqlServer", "normalized-email")]
    [InlineData("PostgreSql", "profile-hash")]
    [InlineData("SqlServer", "profile-hash")]
    [InlineData("PostgreSql", "profile-totp")]
    [InlineData("SqlServer", "profile-totp")]
    [InlineData("PostgreSql", "missing-salt")]
    [InlineData("SqlServer", "missing-salt")]
    public async Task DatabaseConstraints_RejectSecondOwnersAndIncompleteCredentials(string provider, string violation)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);
        var store = CreateStore(db);
        var profile = await store.CreateProfileAsync("owner@example.test", ct);
        var attempted = new OwnerAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = "second@example.test",
            NormalizedEmail = "SECOND@EXAMPLE.TEST",
            Revision = Guid.NewGuid()
        };
        if (violation is "profile-hash" or "profile-totp" or "missing-salt")
        {
            attempted = await db.OwnerAccount.SingleAsync(ct);
            if (violation == "profile-totp") attempted.TotpSecret = "JBSWY3DPEHPK3PXP";
            else attempted.PasswordHash = "synthetic-hash";
            if (violation == "missing-salt") attempted.LocalInitializedUtc = DateTime.UtcNow;
        }
        else
        {
            if (violation == "invalid-slot") attempted.Slot = 2;
            if (violation == "normalized-email")
            {
                // Isolate email uniqueness from the independently tested singleton index.
                await database.ExecuteAsync(provider == "PostgreSql"
                    ? """DROP INDEX public."IX_OwnerAccount_Slot";"""
                    : "DROP INDEX IX_OwnerAccount_Slot ON dbo.OwnerAccount;");
                attempted.NormalizedEmail = "OWNER@EXAMPLE.TEST";
            }
            db.OwnerAccount.Add(attempted);
        }

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));

        db.ChangeTracker.Clear();
        Assert.Equal(profile, await store.ReadAsync(ct));
        Assert.Equal(1, await db.OwnerAccount.CountAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", false)]
    [InlineData("SqlServer", false)]
    [InlineData("PostgreSql", true)]
    [InlineData("SqlServer", true)]
    public async Task ConcurrentInitialization_OnlyOneCommitWins_AndFailedWriteCannotReappear(string provider, bool profileExists)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var schema = database.Context();
        await schema.Database.EnsureCreatedAsync(ct);
        var profile = profileExists
            ? await CreateStore(schema).CreateProfileAsync("profile@example.test", ct) : null;
        var barrier = new ConcurrentWriteBarrier();
        await using var firstDb = IndependentContext(database, barrier);
        await using var secondDb = IndependentContext(database, barrier);
        var firstStore = CreateStore(firstDb, required: true);
        var secondStore = CreateStore(secondDb, required: true);
        var firstPending = await firstStore.PrepareLocalSetupAsync("first@example.test", "FirstSynthetic123!", ct);
        var secondPending = await secondStore.PrepareLocalSetupAsync("second@example.test", "SecondSynthetic123!", ct);
        Assert.Equal(profile?.Revision, firstPending.ExpectedRevision);
        Assert.Equal(profile?.Revision, secondPending.ExpectedRevision);
        Assert.NotEqual(firstPending.TotpSecret, secondPending.TotpSecret);
        var first = firstStore.CompleteLocalSetupAsync(firstPending, Code(firstPending.TotpSecret!), ct);
        var second = secondStore.CompleteLocalSetupAsync(secondPending, Code(secondPending.TotpSecret!), ct);
        try
        {
            var arrival = await Task.WhenAny(barrier.Ready.Task, first, second).WaitAsync(TimeSpan.FromSeconds(30), ct);
            await arrival;
        }
        finally
        {
            barrier.Release.SetResult();
            await Record.ExceptionAsync(() => Task.WhenAll(first, second));
        }
        var tasks = new[] { first, second };
        var winner = await Assert.Single(tasks, task => task.IsCompletedSuccessfully);
        var failure = Assert.Single(tasks, task => task.IsFaulted).Exception!.GetBaseException();
        Assert.IsType<DbUpdateConcurrencyException>(failure);
        if (profileExists) Assert.Equal(profile.Id, winner.Id);
        Assert.Equal(1, await schema.OwnerAccount.CountAsync(ct));
        Assert.Empty(firstDb.ChangeTracker.Entries<OwnerAccountEntity>());
        Assert.Empty(secondDb.ChangeTracker.Entries<OwnerAccountEntity>());

        firstDb.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "UnrelatedFirst", CfgValue = "{}" });
        secondDb.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "UnrelatedSecond", CfgValue = "{}" });
        await firstDb.SaveChangesAsync(ct);
        await secondDb.SaveChangesAsync(ct);
        Assert.Equal(winner, await firstStore.ReadAsync(ct));
        Assert.Equal(winner, await secondStore.ReadAsync(ct));
        var winningPassword = winner.Email == "first@example.test" ? "FirstSynthetic123!" : "SecondSynthetic123!";
        var losingEmail = winner.Email == "first@example.test" ? "second@example.test" : "first@example.test";
        Assert.Equal(winner, await firstStore.AuthenticateLocalAsync(winner.Email, winningPassword, ct));
        Assert.Null(await firstStore.AuthenticateLocalAsync(losingEmail, "FirstSynthetic123!", ct));
        var winnerPending = winner.Email == firstPending.Email ? firstPending : secondPending;
        var saved = await schema.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal(winnerPending.PasswordHash, saved.PasswordHash);
        Assert.Equal(winnerPending.PasswordSalt, saved.PasswordSalt);
        Assert.Equal(winnerPending.TotpSecret, saved.TotpSecret);
        Assert.True(winner.IsTotpEnabled);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task OwnerOnly_PreventsSeeding_AndFullDatabaseClearRemovesAccount(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);
        var store = CreateStore(db);
        var profile = await store.CreateProfileAsync("retained@example.test", ct);
        using var harness = new StartupTestHarness(db);

        Assert.Equal(InitStartUpResult.FailedInitBlogConfig, await harness.Initializer.InitStartUpAsync(ct));
        Assert.Equal(profile, await store.ReadAsync(ct));
        Assert.False(await db.BlogConfiguration.AnyAsync(ct));
        Assert.False(await db.Post.AnyAsync(ct));
        await db.ClearAllData();
        Assert.Null(await store.ReadProfileAsync(ct));
        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        Assert.Single(await db.Post.ToListAsync(ct));
    }

    private static BlogDbContext IndependentContext(CategoryTestDatabase database, params IInterceptor[] interceptors)
    {
        var connection = (DbConnection)((ICloneable)database.Connection).Clone();
        return database.Provider == "PostgreSql"
            ? new PostgreSqlBlogDbContext(new DbContextOptionsBuilder<PostgreSqlBlogDbContext>()
                .UseNpgsql(connection, contextOwnsConnection: true).AddInterceptors(interceptors).Options)
            : new SqlServerBlogDbContext(new DbContextOptionsBuilder<SqlServerBlogDbContext>()
                .UseSqlServer(connection, contextOwnsConnection: true).AddInterceptors(interceptors).Options);
    }

    private static OwnerAccountStore CreateStore(BlogDbContext db, bool required = false) =>
        new(db, new LocalAccountTotpService(), Options.Create(new AuthenticationSettings
        {
            Totp = new TotpAuthenticationSettings { Required = required }
        }), TimeProvider.System, Moq.Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());

    private static string Code(string secret) => new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

    private static async Task<string[]> UnrelatedRowsAsync(BlogDbContext db, CategoryTestDatabase database, bool includeOidc = true)
    {
        var snapshots = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes().Where(entity => entity.ClrType != typeof(OwnerAccountEntity))
                     .OrderBy(entity => entity.GetTableName(), StringComparer.Ordinal))
        {
            if (!includeOidc && (entity.ClrType == typeof(OwnerOidcEntity) ||
                entity.ClrType == typeof(Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey))) continue;
            var table = entity.GetTableName();
            var columns = string.Join(", ", entity.GetProperties().OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => $"\"{property.GetColumnName()}\""));
            var rows = await database.RowsAsync($"SELECT {columns} FROM \"{table}\"");
            snapshots.Add(table + ":" + JsonSerializer.Serialize(rows.Select(row => JsonSerializer.Serialize(row)).Order(StringComparer.Ordinal)));
        }
        return snapshots.ToArray();
    }

    private static async Task<string[]> SchemaAsync(CategoryTestDatabase database)
    {
        var table = database.Provider == "PostgreSql" ? "owner_account" : "OwnerAccount";
        var columns = await database.RowsAsync($"""
            SELECT column_name, data_type, is_nullable, character_maximum_length, datetime_precision
            FROM information_schema.columns WHERE table_name = '{table}' ORDER BY column_name;
            """);
        var indexes = await database.RowsAsync(database.Provider == "PostgreSql" ? """
            SELECT indexname, indexdef FROM pg_indexes
            WHERE schemaname = 'public' AND tablename = 'owner_account' ORDER BY indexname;
            """ : """
            SELECT i.name, i.is_unique, c.name FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(N'dbo.OwnerAccount') ORDER BY i.name, ic.key_ordinal;
            """);
        var constraints = await database.RowsAsync($"""
            SELECT constraint_name, constraint_type FROM information_schema.table_constraints
            WHERE table_name = '{table}' ORDER BY constraint_name;
            """);
        return [JsonSerializer.Serialize(columns), JsonSerializer.Serialize(indexes), JsonSerializer.Serialize(constraints)];
    }

    private sealed class ConcurrentWriteBarrier : SaveChangesInterceptor
    {
        private int _arrivals;
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<OwnerAccountEntity>()
                .Any(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                if (Interlocked.Increment(ref _arrivals) == 2) Ready.SetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }
}
