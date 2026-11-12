using Microsoft.EntityFrameworkCore;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Setup.Tests.TestSupport;
using Moq;
using System.Data.Common;
using System.Text.Json;

namespace Moonglade.Setup.Tests.Database;

public partial class OwnerAccountDatabaseTests
{
    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task PresentationCutover_PreservesAvatarAndRerunPreservesProfileEdits(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await SeedCutoverAsync(db, """{"siteTitle":"Moonglade","OWNERNAME":"Sample owner","description":"Sample bio"}""", ct);
        db.OwnerAvatar.Add(new OwnerAvatarEntity { UseGravatar = true, ImageBase64 = "AQID" });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await database.ExecuteAsync(provider == "PostgreSql"
            ? "ALTER TABLE public.owner_avatar DROP COLUMN name, DROP COLUMN description;"
            : "ALTER TABLE dbo.OwnerAvatar DROP COLUMN Name, Description;");

        await database.MigrateAsync("owner-presentation.sql");

        var row = await db.OwnerAvatar.AsNoTracking().SingleAsync(ct);
        Assert.Equal("Sample owner", row.Name);
        Assert.Equal("Sample bio", row.Description);
        Assert.Equal("AQID", row.ImageBase64);
        Assert.True(row.UseGravatar);
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        var general = await db.BlogConfiguration.AsNoTracking().SingleAsync(setting => setting.CfgKey == nameof(GeneralSettings), ct);
        using var json = JsonDocument.Parse(general.CfgValue);
        Assert.Equal("siteTitle", Assert.Single(json.RootElement.EnumerateObject()).Name);
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), general.LastModifiedTimeUtc);

        var edited = OwnerPresentation.Default with { Name = "Edited name" };
        await new OwnerAvatarStore(db).SetPresentationAsync(edited, ct);
        await database.MigrateAsync("owner-presentation.sql");
        Assert.Equal(edited, await CreateStore(db).ReadPresentationAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task AvatarCutover_MovesBothValues_AndRerunPreservesAccountEdits(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await SeedCutoverAsync(db, """{"siteTitle":"Moonglade","UseGravatarAsProfilePicture":true,"avatarUrl":"/inactive"}""", ct);
        var assetId = Guid.Parse("0922e4dc-b47b-44e2-a493-05d4e624381c");
        db.BlogAsset.Add(new BlogAssetEntity { Id = assetId, Base64Data = "AQID", LastModifiedTimeUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        var before = await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);

        await database.ExecuteAsync(provider == "PostgreSql" ? "DROP TABLE public.owner_avatar;" : "DROP TABLE dbo.OwnerAvatar;");
        await database.MigrateAsync("owner-avatar.sql");

        var avatars = new OwnerAvatarStore(db);
        Assert.True(await avatars.UsesGravatarAsync(ct));
        Assert.Equal("AQID", await avatars.ReadImageAsync(ct));
        Assert.False(await db.BlogAsset.AnyAsync(asset => asset.Id == assetId, ct));
        var after = await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);
        Assert.Equal(before.LastModifiedTimeUtc, after.LastModifiedTimeUtc);
        using var general = JsonDocument.Parse(after.CfgValue);
        var retained = Assert.Single(general.RootElement.EnumerateObject());
        Assert.Equal("siteTitle", retained.Name);
        Assert.Equal("Moonglade", retained.Value.GetString());
        Assert.False(await db.OwnerAccount.AnyAsync(ct));

        await database.ExecuteAsync(provider == "PostgreSql"
            ? "UPDATE public.owner_avatar SET use_gravatar = false WHERE id = 1;"
            : "UPDATE dbo.OwnerAvatar SET UseGravatar = 0 WHERE Id = 1;");
        await database.MigrateAsync("owner-avatar.sql");

        Assert.False(await avatars.UsesGravatarAsync(ct));
        Assert.Equal("AQID", await avatars.ReadImageAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task Cutover_EmailAtMaximumLength_RemainsUsableForActualLocalSetup(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        var email = new string('a', 241) + "@example.test";
        Assert.Equal(254, email.Length);
        await SeedCutoverAsync(db, LegacyGeneral("ownerEmail", JsonSerializer.Serialize(email)), ct);

        await database.MigrateAsync("owner-account-cutover.sql");

        var store = CreateStore(db);
        var profile = await store.ReadAsync(ct);
        Assert.Equal(email, profile!.Email);
        var pending = await store.PrepareLocalSetupAsync(email, "Synthetic123!", ct);
        var initialized = await store.CompleteLocalSetupAsync(pending, null, ct);
        Assert.Equal(profile.Id, initialized.Id);
        Assert.Equal(initialized, await store.AuthenticateLocalAsync(email.ToUpperInvariant(), "Synthetic123!", ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task Cutover_DuplicateLegacyEmail_WithConfiguredAccount_RetainsCredentialsAndEveryOriginalRow(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await SeedCutoverAsync(db, LegacyGeneral("ownerEmail", "\"one@example.test\"")[..^1]
            + ",\"OwnerEmail\":\"two@example.test\"}", ct);
        var store = CreateStore(db, true);
        var owner = await InitializeWithTotpAsync(store, ct);
        var credentials = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        var before = await UnrelatedRowsAsync(db, database);

        await Assert.ThrowsAnyAsync<DbException>(() => database.MigrateAsync("owner-account-cutover.sql"));

        Assert.Equal(before, await UnrelatedRowsAsync(db, database));
        Assert.Equal(owner, await store.AuthenticateLocalAsync(owner.Email, "Synthetic123!", ct));
        var saved = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal((credentials.PasswordHash, credentials.PasswordSalt, credentials.TotpSecret, credentials.LocalInitializedUtc),
            (saved.PasswordHash, saved.PasswordSalt, saved.TotpSecret, saved.LocalInitializedUtc));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", true)]
    [InlineData("SqlServer", true)]
    [InlineData("PostgreSql", false)]
    [InlineData("SqlServer", false)]
    public async Task Cutover_LegacyAndAdditiveSchemas_PreservePublicProfileContentHistoryAndTimestamps(string provider, bool legacySchema)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await SeedCutoverAsync(db, LegacyGeneral("OwnerEMAIL", "\" Legacy@example.test \""), ct);
        var before = await CutoverUnrelatedRowsAsync(db, database);
        var general = await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);
        if (legacySchema)
            await database.ExecuteAsync(provider == "PostgreSql" ? "DROP TABLE public.owner_account;" : "DROP TABLE dbo.OwnerAccount;");
        await database.MigrateAsync("owner-account.sql");

        await database.MigrateAsync("owner-account-cutover.sql");

        var store = CreateStore(db);
        var migrated = await store.ReadAsync(ct);
        Assert.Equal("Legacy@example.test", migrated!.Email);
        Assert.False(migrated.IsLocalInitialized);
        Assert.False(migrated.IsTotpEnabled);
        Assert.Equal(new OwnerProfile(migrated.Id, migrated.Email), await store.ReadProfileAsync(ct));
        Assert.Null(await store.AuthenticateLocalAsync(migrated.Email, "Synthetic123!", ct));
        var ownerRow = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal("LEGACY@EXAMPLE.TEST", ownerRow.NormalizedEmail);
        Assert.Null(ownerRow.PasswordHash);
        Assert.Null(ownerRow.PasswordSalt);
        Assert.Null(ownerRow.LocalInitializedUtc);
        Assert.Null(ownerRow.TotpSecret);
        await AssertCutoverPreservedAsync(db, database, before, general, ct);
        var firstRun = (await UnrelatedRowsAsync(db, database), ownerRow.Revision);

        await database.MigrateAsync("owner-account-cutover.sql");

        Assert.Equal(migrated, await store.ReadAsync(ct));
        Assert.Equal(firstRun.Item1, await UnrelatedRowsAsync(db, database));
        await using var runtime = database.Context();
        using var harness = new StartupTestHarness(runtime);
        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
        Assert.Equal(before, await CutoverUnrelatedRowsAsync(runtime, database));
        var pending = await CreateStore(runtime).PrepareLocalSetupAsync("confirmed@example.test", "Synthetic123!", ct);
        var initialized = await CreateStore(runtime).CompleteLocalSetupAsync(pending, null, ct);
        Assert.Equal(migrated.Id, initialized.Id);
        Assert.Equal(initialized, await CreateStore(runtime).AuthenticateLocalAsync("CONFIRMED@example.test", "Synthetic123!", ct));
        await database.MigrateAsync("owner-account-cutover.sql");
        Assert.Equal(initialized, await CreateStore(runtime).ReadAsync(ct));
        Assert.Equal(before, await CutoverUnrelatedRowsAsync(runtime, database));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", false)]
    [InlineData("SqlServer", false)]
    [InlineData("PostgreSql", true)]
    [InlineData("SqlServer", true)]
    public async Task Cutover_AlreadyConfiguredProfileOrAccount_RerunsPreserveCredentialsAndLaterEmailEdits(string provider, bool initialized)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await SeedCutoverAsync(db, LegacyGeneral("ownerEmail", "\"invalid legacy address\""), ct);
        var store = CreateStore(db, initialized);
        var original = initialized
            ? await InitializeWithTotpAsync(store, ct)
            : await store.CreateProfileAsync("configured@example.test", ct);
        var credentials = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        var unrelated = await CutoverUnrelatedRowsAsync(db, database);

        await database.MigrateAsync("owner-account-cutover.sql");

        Assert.Equal(original, await store.ReadAsync(ct));
        var changed = initialized
            ? (await store.ChangeLocalEmailAsync(original.Id, original.Revision, "Synthetic123!", "edited@example.test", ct))!
            : original;
        for (var run = 0; run < 2; run++)
        {
            await database.MigrateAsync("owner-account.sql");
            await database.MigrateAsync("owner-account-cutover.sql");
            Assert.Equal(changed, await store.ReadAsync(ct));
            var saved = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
            Assert.Equal((credentials.Id, credentials.PasswordHash, credentials.PasswordSalt, credentials.TotpSecret, credentials.LocalInitializedUtc),
                (saved.Id, saved.PasswordHash, saved.PasswordSalt, saved.TotpSecret, saved.LocalInitializedUtc));
            Assert.Equal(unrelated, await CutoverUnrelatedRowsAsync(db, database));
        }
        if (initialized)
        {
            Assert.Equal(changed, await store.AuthenticateLocalAsync(changed.Email, "Synthetic123!", ct));
            Assert.Equal(changed, await store.VerifyTotpAsync(changed.Id, changed.Revision, Code(credentials.TotpSecret!), ct));
        }
        else
        {
            Assert.Null(await store.AuthenticateLocalAsync(changed.Email, "Synthetic123!", ct));
            var pending = await store.PrepareLocalSetupAsync(changed.Email, "Synthetic123!", ct);
            var completed = await store.CompleteLocalSetupAsync(pending, null, ct);

            await using var reopened = database.Context();
            Assert.Equal(original.Id, completed.Id);
            Assert.Equal(original.Email, completed.Email);
            Assert.Equal(completed, await CreateStore(reopened).AuthenticateLocalAsync(completed.Email, "Synthetic123!", ct));
            Assert.Equal(unrelated, await CutoverUnrelatedRowsAsync(reopened, database));
        }
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", "invalid-json")]
    [InlineData("SqlServer", "invalid-json")]
    [InlineData("PostgreSql", "non-object")]
    [InlineData("SqlServer", "non-object")]
    [InlineData("PostgreSql", "invalid-email")]
    [InlineData("SqlServer", "invalid-email")]
    [InlineData("PostgreSql", "duplicate-email")]
    [InlineData("SqlServer", "duplicate-email")]
    [InlineData("PostgreSql", "missing-general")]
    [InlineData("SqlServer", "missing-general")]
    [InlineData("PostgreSql", "non-string-email")]
    [InlineData("SqlServer", "non-string-email")]
    [InlineData("PostgreSql", "line-break-email")]
    [InlineData("SqlServer", "line-break-email")]
    [InlineData("PostgreSql", "overlong-email")]
    [InlineData("SqlServer", "overlong-email")]
    public async Task Cutover_InvalidInput_FailsBeforeAnyLegacyOrUnrelatedDataIsRemoved(string provider, string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        var json = scenario switch
        {
            "invalid-json" => "{",
            "non-object" => "[]",
            "invalid-email" => LegacyGeneral("OwnerEmail", "\"not-an-address\""),
            "non-string-email" => LegacyGeneral("OwnerEmail", "42"),
            "line-break-email" => LegacyGeneral("OwnerEmail", "\"owner@example.test\\r\\n\""),
            "overlong-email" => LegacyGeneral("OwnerEmail", JsonSerializer.Serialize(new string('a', 242) + "@example.test")),
            "duplicate-email" => LegacyGeneral("ownerEmail", "\"one@example.test\"")[..^1] + ",\"OWNEREMAIL\":\"two@example.test\"}",
            _ => LegacyGeneral("ownerEmail", "\"legacy@example.test\"")
        };
        await SeedCutoverAsync(db, json, ct);
        if (scenario == "missing-general")
            await db.BlogConfiguration.Where(row => row.CfgKey == nameof(GeneralSettings)).ExecuteDeleteAsync(ct);
        var before = await UnrelatedRowsAsync(db, database);

        await Assert.ThrowsAnyAsync<DbException>(() => database.MigrateAsync("owner-account-cutover.sql"));

        Assert.Equal(before, await UnrelatedRowsAsync(db, database));
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        Assert.True(await db.BlogConfiguration.AnyAsync(row => row.CfgKey == "LocalAccountSettings", ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", "lowercase")]
    [InlineData("SqlServer", "lowercase")]
    [InlineData("PostgreSql", "trailing-space")]
    [InlineData("SqlServer", "trailing-space")]
    public async Task Cutover_LegacyAccountKeyLookalike_IsNotDeleted(string provider, string variant)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await SeedCutoverAsync(db, GeneralSettings.DefaultValue.ToJson(), ct);
        await db.BlogConfiguration.Where(row => row.CfgKey == "LocalAccountSettings").ExecuteDeleteAsync(ct);
        var key = variant == "lowercase" ? "localaccountsettings" : "LocalAccountSettings ";
        db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = key, CfgValue = """{"retained":true}""" });
        await db.SaveChangesAsync(ct);
        var before = await UnrelatedRowsAsync(db, database);

        await database.MigrateAsync("owner-account-cutover.sql");
        await database.MigrateAsync("owner-account-cutover.sql");

        Assert.Equal(before, await UnrelatedRowsAsync(db, database));
        Assert.Null(await CreateStore(db).ReadProfileAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task Cutover_AlreadyCleanGeneralWithoutProfile_RemainsUninitializedAndKeepsOrdinaryState(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await SeedCutoverAsync(db, GeneralSettings.DefaultValue.ToJson(), ct);
        var before = await CutoverUnrelatedRowsAsync(db, database);
        for (var run = 0; run < 2; run++)
        {
            await database.MigrateAsync("owner-account-cutover.sql");
            Assert.Null(await CreateStore(db).ReadProfileAsync(ct));
            Assert.Equal(before, await CutoverUnrelatedRowsAsync(db, database));
        }
        using var harness = new StartupTestHarness(db);
        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
        Assert.Null(await CreateStore(db).ReadAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task ConcurrentLocalEmailChanges_OnlyOneRevisionWins(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var schema = database.Context();
        await schema.Database.EnsureCreatedAsync(ct);
        var original = await InitializeWithTotpAsync(CreateStore(schema, required: true), ct);
        var barrier = new ConcurrentWriteBarrier();
        await using var firstDb = IndependentContext(database, barrier);
        await using var secondDb = IndependentContext(database, barrier);
        var first = CreateStore(firstDb).ChangeLocalEmailAsync(original.Id, original.Revision, "Synthetic123!", "first@example.test", ct);
        var second = CreateStore(secondDb).ChangeLocalEmailAsync(original.Id, original.Revision, "Synthetic123!", "second@example.test", ct);
        try
        {
            await await Task.WhenAny(barrier.Ready.Task, first, second).WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        finally
        {
            barrier.Release.TrySetResult();
            await Record.ExceptionAsync(() => Task.WhenAll(first, second));
        }
        var tasks = new[] { first, second };
        var winner = await Assert.Single(tasks, task => task.IsCompletedSuccessfully);
        Assert.IsType<DbUpdateConcurrencyException>(Assert.Single(tasks, task => task.IsFaulted).Exception!.GetBaseException());
        Assert.Equal(winner, await CreateStore(schema).ReadAsync(ct));
        Assert.Single(await schema.OwnerAccount.ToListAsync(ct));
        foreach (var context in new[] { firstDb, secondDb })
        {
            Assert.Empty(context.ChangeTracker.Entries<OwnerAccountEntity>());
            context.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = Guid.NewGuid().ToString(), CfgValue = "{}" });
            await context.SaveChangesAsync(ct);
        }
        Assert.Equal(winner, await CreateStore(schema).ReadAsync(ct));
        Assert.Equal(original.Id, winner!.Id);
    }

    private static async Task<OwnerAccountSnapshot> InitializeWithTotpAsync(OwnerAccountStore store, CancellationToken ct)
    {
        var pending = await store.PrepareLocalSetupAsync("configured@example.test", "Synthetic123!", ct);
        return await store.CompleteLocalSetupAsync(pending, Code(pending.TotpSecret!), ct);
    }

    private static string LegacyGeneral(string key, string value) =>
        GeneralSettings.DefaultValue.ToJson()[..^1] + ",\"ownerName\":\"Retained owner\",\"description\":\"Retained description\""
        + $",\"{key}\":{value},\"futureObject\":{{\"ownerEmail\":\"nested@example.test\",\"enabled\":false}},\"futureArray\":[1,null,\"literal space\"]}}";

    private static async Task SeedCutoverAsync(BlogDbContext db, string generalJson, CancellationToken ct)
    {
        using var harness = new StartupTestHarness(db);
        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
        var general = await db.BlogConfiguration.SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);
        general.CfgValue = generalJson;
        general.LastModifiedTimeUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        db.BlogConfiguration.AddRange(
            new BlogConfigurationEntity { CfgKey = "LocalAccountSettings", CfgValue = """{"username":"old-admin","passwordHash":"inert-legacy","totpSecret":"inert-legacy"}""" },
            new BlogConfigurationEntity { CfgKey = "UnrelatedLegacyGroup", CfgValue = """{"retained":"literal space","enabled":false}""" });
        db.Comment.Add(new CommentEntity
        {
            Id = Guid.NewGuid(),
            PostId = (await db.Post.SingleAsync(ct)).Id,
            Username = "Retained visitor",
            Email = "visitor@example.test",
            IPAddress = "192.0.2.1",
            CommentContent = "Retained comment",
            CreateTimeUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        });
        db.ActivityLog.Add(new ActivityLogEntity
        {
            EventKey = "post.created",
            EventTimeUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            ActorKind = ActivityActorKind.Administrator,
            ActorDisplayName = "Historical owner",
            TargetDisplayName = "Retained post",
            Details = "{}"
        });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    private static async Task<string[]> CutoverUnrelatedRowsAsync(BlogDbContext db, CategoryTestDatabase database)
    {
        var values = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes().Where(entity => entity.ClrType != typeof(OwnerAccountEntity))
                     .OrderBy(entity => entity.GetTableName(), StringComparer.Ordinal))
        {
            var columns = string.Join(", ", entity.GetProperties().OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => $"\"{property.GetColumnName()}\""));
            var where = entity.ClrType == typeof(BlogConfigurationEntity)
                ? $" WHERE \"{(database.Provider == "PostgreSql" ? "cfg_key" : "CfgKey")}\" NOT IN ('GeneralSettings', 'LocalAccountSettings')" : "";
            var rows = await database.RowsAsync($"SELECT {columns} FROM \"{entity.GetTableName()}\"{where}");
            values.Add(entity.GetTableName() + ":" + JsonSerializer.Serialize(rows.Select(row => JsonSerializer.Serialize(row)).Order(StringComparer.Ordinal)));
        }
        return values.ToArray();
    }

    private static async Task AssertCutoverPreservedAsync(BlogDbContext db, CategoryTestDatabase database,
        string[] unrelated, BlogConfigurationEntity original, CancellationToken ct)
    {
        Assert.Equal(unrelated, await CutoverUnrelatedRowsAsync(db, database));
        Assert.False(await db.BlogConfiguration.AnyAsync(row => row.CfgKey == "LocalAccountSettings", ct));
        var general = await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);
        Assert.Equal(original.LastModifiedTimeUtc, general.LastModifiedTimeUtc);
        using var before = JsonDocument.Parse(original.CfgValue);
        using var after = JsonDocument.Parse(general.CfgValue);
        var retained = before.RootElement.EnumerateObject().Where(property => !property.Name.Equals("OwnerEmail", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Equal(retained.Select(property => property.Name), after.RootElement.EnumerateObject().Select(property => property.Name));
        foreach (var property in retained) Assert.True(JsonElement.DeepEquals(property.Value, after.RootElement.GetProperty(property.Name)));
        Assert.Equal("Retained owner", after.RootElement.GetProperty("ownerName").GetString());
        Assert.Equal("nested@example.test", after.RootElement.GetProperty("futureObject").GetProperty("ownerEmail").GetString());
    }
}
