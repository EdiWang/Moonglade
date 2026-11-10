using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Setup.Tests.TestSupport;
using Moq;
using System.Data.Common;
using System.Globalization;

namespace Moonglade.Setup.Tests.Database;

[Collection("Startup site icon cache")]
public class ActivityLogDatabaseTests(DisposableCategoryDatabases databases) : IClassFixture<DisposableCategoryDatabases>
{
    private const string Script = "activity-log-events.sql";

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task FreshSchema_EnsureCreatedAndSeed_UsesCanonicalShapeAndRealHandlers(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);
        await Seed.SeedAsync(db, Mock.Of<ILogger>());
        Assert.Equal("welcome-to-moonglade", (await db.Post.AsNoTracking().SingleAsync(ct)).Slug);
        Assert.Equal(100, (await db.Category.AsNoTracking().SingleAsync(ct)).Id);
        await AssertCanonicalSchemaAsync(database);
        await ExerciseHandlersAsync(db);
        await UpgradeAndReconnectAsync(database);
        var preserved = await db.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.Equal("activity-log.cleared", preserved.EventKey);
        Assert.Equal("post-upgrade-admin", preserved.ActorDisplayName);
        Assert.Equal(1, await db.Post.CountAsync(ct));
        Assert.Equal(1, await db.Category.CountAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task LegacyUpgrade_DiscardsOnlyOldHistory_RerunPreservesNewRowsIdentityAndUnrelatedData(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);
        await Seed.SeedAsync(db, Mock.Of<ILogger>());
        var settingTime = new DateTime(2026, 1, 1, 1, 2, 3, DateTimeKind.Utc);
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "B2UnrelatedSentinel",
            CfgValue = "{\"preserve\":\"unrelated-setting\"}",
            LastModifiedTimeUtc = settingTime
        });
        await db.SaveChangesAsync(ct);
        var posts = (await db.Post.AsNoTracking().ToListAsync(ct)).Select(row => (row.Id, row.Title, row.PostContent, row.CreateTimeUtc)).ToArray();
        var categories = (await db.Category.AsNoTracking().ToListAsync(ct)).Select(row => (row.Id, row.DisplayName, row.Slug, row.Note)).ToArray();
        await ReplaceWithLegacyAsync(database);
        var oldRows = await LegacyRowsAsync(database);
        Assert.Equal(2, oldRows.Length);
        Assert.Equal(["legacy-one", "legacy-two"], oldRows.Select(row => row[1]).ToArray());

        await UpgradeAndReconnectAsync(database);
        await AssertCanonicalSchemaAsync(database);
        Assert.Empty(await db.ActivityLog.AsNoTracking().ToListAsync(ct));
        db.ChangeTracker.Clear();
        await Writer(db).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostCreated, ActivityActorKind.Administrator,
            "new-admin", "snapshot-id", "New captured title",
            ActivityLogDetails.Create(("PostStatus", PostStatus.Draft), ("Slug", "safe-slug"))), ct);
        var beforeRerun = await db.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.True(beforeRerun.Id > 2);
        Assert.Equal(DateTimeKind.Utc, beforeRerun.EventTimeUtc.Kind);
        await UpgradeAndReconnectAsync(database);
        var afterRerun = await db.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.Equal((beforeRerun.Id, beforeRerun.EventKey, beforeRerun.EventTimeUtc, beforeRerun.ActorKind,
                beforeRerun.ActorDisplayName, beforeRerun.TargetId, beforeRerun.TargetDisplayName, beforeRerun.Details),
            (afterRerun.Id, afterRerun.EventKey, afterRerun.EventTimeUtc, afterRerun.ActorKind,
                afterRerun.ActorDisplayName, afterRerun.TargetId, afterRerun.TargetDisplayName, afterRerun.Details));
        await Writer(db).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostPublished, ActivityActorKind.System,
            "discarded", "second-id", "Second title", IpAddress: "192.0.2.88", UserAgent: "discarded"), ct);
        var second = await db.ActivityLog.AsNoTracking().SingleAsync(row => row.EventKey == ActivityEvents.PostPublished, ct);
        Assert.Equal(beforeRerun.Id + 1, second.Id);
        Assert.Null(second.ActorDisplayName);
        Assert.Null(second.IpAddress);
        Assert.Null(second.UserAgent);
        Assert.Equal(posts, (await db.Post.AsNoTracking().ToListAsync(ct)).Select(row => (row.Id, row.Title, row.PostContent, row.CreateTimeUtc)).ToArray());
        Assert.Equal(categories, (await db.Category.AsNoTracking().ToListAsync(ct)).Select(row => (row.Id, row.DisplayName, row.Slug, row.Note)).ToArray());
        var setting = await db.BlogConfiguration.AsNoTracking().SingleAsync(ct);
        Assert.Equal("B2UnrelatedSentinel", setting.CfgKey);
        Assert.Equal("{\"preserve\":\"unrelated-setting\"}", setting.CfgValue);
        Assert.Equal(settingTime, setting.LastModifiedTimeUtc);
        await ExerciseHandlersAsync(db);
        Assert.Equal(1, await db.Post.CountAsync(ct));
        Assert.Equal(1, await db.BlogConfiguration.CountAsync(ct));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task CompleteLegacyInstallation_UpgradeAndRerun_StartUpPreservesSettingsContentAndIdentity(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        var settingTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        using (var installed = new StartupTestHarness(db))
        {
            Assert.Equal(InitStartUpResult.Success, await installed.Initializer.InitStartUpAsync(ct));
            var general = await db.BlogConfiguration.SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);
            general.CfgValue = (installed.Config.Capture().GeneralSettings with { SiteTitle = "B5 retained" }).ToJson();
            general.LastModifiedTimeUtc = settingTime;
            db.Comment.Add(new CommentEntity
            {
                Id = Guid.NewGuid(),
                PostId = (await db.Post.SingleAsync(ct)).Id,
                Username = "B5 retained visitor",
                Email = "b5@example.invalid",
                IPAddress = "192.0.2.55",
                CommentContent = "B5 retained comment",
                CreateTimeUtc = settingTime,
                IsApproved = true
            });
            await db.SaveChangesAsync(ct);
        }
        db.ChangeTracker.Clear();
        Assert.Equal(12, await db.BlogConfiguration.CountAsync(ct));
        Assert.Equal(1, await db.Post.CountAsync(ct));
        Assert.Equal(100, (await db.Category.AsNoTracking().SingleAsync(ct)).Id);
        Assert.Equal(2, await db.Tag.CountAsync(ct));
        Assert.Equal(1, await db.Widget.CountAsync(ct));
        Assert.Equal(1, await db.BlogPage.CountAsync(ct));
        Assert.Equal(1, await db.Comment.CountAsync(ct));
        var manifest = (await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(SystemManifestSettings), ct)).CfgValue;
        var before = await UnrelatedRowsAsync(db, database);
        await ReplaceWithLegacyAsync(database);
        Assert.Equal(2, (await LegacyRowsAsync(database)).Length);

        await UpgradeAndReconnectAsync(database);
        await AssertCanonicalSchemaAsync(database);
        Assert.Empty(await db.ActivityLog.AsNoTracking().ToListAsync(ct));
        Assert.Equal(before, await UnrelatedRowsAsync(db, database));
        using (var upgraded = new StartupTestHarness(db))
        {
            Assert.Equal(InitStartUpResult.Success, await upgraded.Initializer.InitStartUpAsync(ct));
            Assert.Equal("B5 retained", upgraded.Config.Capture().GeneralSettings.SiteTitle);
            Assert.Equal(Moonglade.Auth.LocalAccountSettings.DefaultValue.ToJson(), upgraded.Accounts.Read().ToJson());
            Assert.Equal(manifest, upgraded.Manifest.ToJson());
            Assert.DoesNotContain(upgraded.ConfigLogger.Entries, entry => entry.Level >= LogLevel.Warning);
        }
        Assert.Equal(before, await UnrelatedRowsAsync(db, database));
        var occurrence = new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        await Writer(db).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostAutomaticallyPublished, ActivityActorKind.System, null,
            TargetId: "b5-captured-post", TargetDisplayName: "B5 captured title",
            Details: ActivityLogDetails.Create(("PostId", Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))),
            EventTimeUtc: occurrence), ct);
        var written = await db.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.True(written.Id > 2);
        using var writtenDetails = System.Text.Json.JsonDocument.Parse(written.Details!);
        Assert.Equal(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), writtenDetails.RootElement.GetProperty("PostId").GetGuid());
        Assert.Single(writtenDetails.RootElement.EnumerateObject());

        await UpgradeAndReconnectAsync(database);
        await using var restartedDb = database.Context();
        using (var restarted = new StartupTestHarness(restartedDb))
        {
            Assert.Equal(InitStartUpResult.Success, await restarted.Initializer.InitStartUpAsync(ct));
            Assert.Equal("B5 retained", restarted.Config.Capture().GeneralSettings.SiteTitle);
            Assert.Equal(Moonglade.Auth.LocalAccountSettings.DefaultValue.ToJson(), restarted.Accounts.Read().ToJson());
            Assert.Equal(manifest, restarted.Manifest.ToJson());
            Assert.DoesNotContain(restarted.ConfigLogger.Entries, entry => entry.Level >= LogLevel.Warning);
        }
        Assert.Equal(before, await UnrelatedRowsAsync(restartedDb, database));
        var retained = await restartedDb.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.Equal((written.Id, ActivityEvents.PostAutomaticallyPublished, occurrence, ActivityActorKind.System,
                (string)null, "b5-captured-post", "B5 captured title", written.Details, (string)null, (string)null),
            (retained.Id, retained.EventKey, retained.EventTimeUtc, retained.ActorKind, retained.ActorDisplayName,
                retained.TargetId, retained.TargetDisplayName, retained.Details, retained.IpAddress, retained.UserAgent));
        await Writer(restartedDb).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostCreated, ActivityActorKind.Administrator, "b5-admin"), ct);
        Assert.Equal(written.Id + 1, (await restartedDb.ActivityLog.AsNoTracking().SingleAsync(row => row.EventKey == ActivityEvents.PostCreated, ct)).Id);
        var nextCategory = new CategoryEntity { DisplayName = "B5 next category", Slug = "b5-next", Note = "B5 synthetic" };
        restartedDb.Category.Add(nextCategory);
        await restartedDb.SaveChangesAsync(ct);
        Assert.Equal(101, nextCategory.Id);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", "unknown")]
    [InlineData("SqlServer", "unknown")]
    [InlineData("PostgreSql", "partial")]
    [InlineData("SqlServer", "partial")]
    [InlineData("PostgreSql", "wrong-length")]
    [InlineData("SqlServer", "wrong-length")]
    public async Task UnsupportedSchema_RejectsBeforeDeletingLegacyOrUnrelatedSentinels(string provider, string shape)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);
        await Seed.SeedAsync(db, Mock.Of<ILogger>());
        var post = await db.Post.AsNoTracking().SingleAsync(ct);
        await ReplaceWithLegacyAsync(database);
        var before = await LegacyRowsAsync(database);
        var alter = (provider, shape) switch
        {
            ("PostgreSql", "unknown") => "ALTER TABLE public.activity_log ADD COLUMN mystery integer;",
            ("SqlServer", "unknown") => "ALTER TABLE dbo.ActivityLog ADD Mystery int NULL;",
            ("PostgreSql", "partial") => "ALTER TABLE public.activity_log DROP COLUMN operation;",
            ("SqlServer", "partial") => "ALTER TABLE dbo.ActivityLog DROP COLUMN Operation;",
            ("PostgreSql", _) => "ALTER TABLE public.activity_log ALTER COLUMN actor_id TYPE varchar(99);",
            _ => "ALTER TABLE dbo.ActivityLog ALTER COLUMN ActorId nvarchar(99) NULL;"
        };
        await database.ExecuteAsync(alter);
        var failure = await Record.ExceptionAsync(() => database.MigrateAsync(Script));
        Assert.IsAssignableFrom<DbException>(failure);
        Assert.Contains("No history was deleted", failure!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before.Select(row => string.Join("|", row)), (await LegacyRowsAsync(database)).Select(row => string.Join("|", row)));
        var preserved = await db.Post.AsNoTracking().SingleAsync(ct);
        Assert.Equal((post.Id, post.Title, post.PostContent, post.CreateTimeUtc),
            (preserved.Id, preserved.Title, preserved.PostContent, preserved.CreateTimeUtc));
        Assert.Equal(100, (await db.Category.AsNoTracking().SingleAsync(ct)).Id);
    }

    private static async Task ExerciseHandlersAsync(BlogDbContext db)
    {
        var ct = TestContext.Current.CancellationToken;
        await new ClearActivityLogsCommandHandler(db, Mock.Of<ILogger<ClearActivityLogsCommandHandler>>())
            .HandleAsync(new(), ct);
        db.ChangeTracker.Clear();
        var nextMidnightUtc = new DateTime(2026, 4, 1, 16, 0, 0, DateTimeKind.Utc);
        var start = nextMidnightUtc.AddTicks(-10);
        ActivityLogEntity[] rows =
        [
            new()
            {
                EventKey = ActivityEvents.PostCreated, EventTimeUtc = start, ActorKind = ActivityActorKind.Administrator,
                ActorDisplayName = "System", TargetId = "post-id", TargetDisplayName = "Captured post",
                IpAddress = "192.0.2.60", UserAgent = "synthetic-agent",
                Details = "{\"PostStatus\":\"Draft\",\"Slug\":\"captured\"}"
            },
            new() { EventKey = ActivityEvents.CommentDeleted, EventTimeUtc = start, ActorKind = ActivityActorKind.System, Details = "{\"CommentIds\":[1,2]}" },
            new() { EventKey = ActivityEvents.PostCreated, EventTimeUtc = nextMidnightUtc, ActorKind = ActivityActorKind.System }
        ];
        db.AddRange(rows);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var listHandler = new ListActivityLogsQueryHandler(db);
        var first = await listHandler.HandleAsync(new(2, 1), ct);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(new[] { rows[2].Id, rows[1].Id }, first.Logs.Select(row => row.Id));
        var second = await listHandler.HandleAsync(new(2, 2), ct);
        Assert.Equal(rows[0].Id, Assert.Single(second.Logs).Id);
        Assert.Equal(DateTimeKind.Utc, second.Logs[0].EventTimeUtc.Kind);
        var filtered = await listHandler.HandleAsync(new(10, 1, [ActivityEvents.PostCreated, ActivityEvents.PostCreated], start, start), ct);
        var item = Assert.Single(filtered.Logs);
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal(rows[0].Id, item.Id);
        Assert.Equal("System", item.ActorDisplayName);
        Assert.Equal(ActivityActorKind.Administrator, item.ActorKind);
        var throughEndOfDay = await listHandler.HandleAsync(new(EndTimeUtc: nextMidnightUtc.AddTicks(-1)), ct);
        Assert.Equal(new[] { rows[1].Id, rows[0].Id }, throughEndOfDay.Logs.Select(row => row.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => listHandler.HandleAsync(new(EventKeys: ["unknown"]), ct));
        var detailHandler = new GetMetaDataByActivityLogIdQueryHandler(db);
        var detail = await detailHandler.HandleAsync(new(rows[0].Id), ct);
        Assert.Equal("post-id", detail!.TargetId);
        Assert.Equal("Captured post", detail.TargetDisplayName);
        Assert.Equal("Draft", detail.Details["PostStatus"].GetString());
        Assert.Equal("captured", detail.Details["Slug"].GetString());
        var empty = await detailHandler.HandleAsync(new(rows[2].Id), ct);
        Assert.NotNull(empty);
        Assert.Empty(empty.Details);
        Assert.Null(await detailHandler.HandleAsync(new(rows[2].Id + 1), ct));
        var deleted = await new ClearActivityLogsCommandHandler(db, Mock.Of<ILogger<ClearActivityLogsCommandHandler>>())
            .HandleAsync(new(), ct);
        Assert.Equal(3, deleted);
        Assert.Empty(await db.ActivityLog.AsNoTracking().ToListAsync(ct));
        await Writer(db).HandleAsync(new CreateActivityLogCommand(ActivityEvents.ActivityLogsCleared, ActivityActorKind.Administrator,
            "post-upgrade-admin", Details: ActivityLogDetails.Create(("DeletedCount", deleted))), ct);
        var audit = await db.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.True(audit.Id > rows[2].Id);
        Assert.Equal("activity-log.cleared", audit.EventKey);
        Assert.Equal(DateTimeKind.Utc, audit.EventTimeUtc.Kind);
        var auditDetail = await detailHandler.HandleAsync(new(audit.Id), ct);
        Assert.Equal(3, auditDetail!.Details["DeletedCount"].GetInt32());
    }

    private static CreateActivityLogCommandHandler Writer(BlogDbContext db) =>
        new(db, Mock.Of<ILogger<CreateActivityLogCommandHandler>>());

    private static async Task<string[]> UnrelatedRowsAsync(BlogDbContext db, CategoryTestDatabase database)
    {
        var tables = db.Model.GetEntityTypes().Select(type => type.GetTableName()!)
            .Where(table => !table.Equals(database.Provider == "PostgreSql" ? "activity_log" : "ActivityLog", StringComparison.Ordinal))
            .Distinct().Order(StringComparer.Ordinal);
        var snapshots = new List<string>();
        foreach (var table in tables)
        {
            await using var command = database.Connection.CreateCommand();
            command.CommandText = database.Provider == "PostgreSql" ? $"SELECT * FROM public.\"{table}\";" : $"SELECT * FROM dbo.[{table}];";
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            var rows = new List<string>();
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                var values = Enumerable.Range(0, reader.FieldCount).Select(index => reader.GetValue(index) switch
                {
                    DBNull => null,
                    byte[] bytes => Convert.ToHexString(bytes),
                    DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                    var value => Convert.ToString(value, CultureInfo.InvariantCulture)
                }).ToArray();
                rows.Add(System.Text.Json.JsonSerializer.Serialize(values));
            }
            snapshots.Add(table + ":" + System.Text.Json.JsonSerializer.Serialize(rows.Order(StringComparer.Ordinal)));
        }
        return snapshots.ToArray();
    }

    private static async Task UpgradeAndReconnectAsync(CategoryTestDatabase database)
    {
        await database.MigrateAsync(Script);
        // The application starts in a new session after the standalone manual SQL upgrade.
        await database.Connection.CloseAsync();
        await database.Connection.OpenAsync(TestContext.Current.CancellationToken);
    }

    private static Task<string[][]> LegacyRowsAsync(CategoryTestDatabase database) =>
        database.RowsAsync(database.Provider == "PostgreSql"
            ? "SELECT id, actor_id, event_id, meta_data FROM public.activity_log ORDER BY id;"
            : "SELECT Id, ActorId, EventId, MetaData FROM dbo.ActivityLog ORDER BY Id;");

    private static async Task ReplaceWithLegacyAsync(CategoryTestDatabase database)
    {
        await database.ExecuteAsync(database.Provider == "PostgreSql"
            ? "DROP TABLE public.activity_log;" : "DROP TABLE dbo.ActivityLog;");
        await database.ExecuteAsync(database.Provider == "PostgreSql" ? """
            CREATE TABLE public.activity_log (
                id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                event_id integer NOT NULL, event_time_utc timestamp with time zone NULL,
                actor_id varchar(100) NULL, operation varchar(100) NULL, target_name varchar(200) NULL,
                meta_data text NULL, ip_address varchar(50) NULL, user_agent varchar(512) NULL);
            INSERT INTO public.activity_log (event_id,event_time_utc,actor_id,operation,target_name,meta_data)
                VALUES (205,'2026-01-01T00:00:00Z','legacy-one','Old create','Old target','{"legacy":1}'),
                       (301,NULL,'legacy-two','Old approval','Old comment','{"legacy":2}');
            """ : """
            CREATE TABLE dbo.ActivityLog (
                Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                EventId int NOT NULL, EventTimeUtc datetime2(7) NULL,
                ActorId nvarchar(100) NULL, Operation nvarchar(100) NULL, TargetName nvarchar(200) NULL,
                MetaData nvarchar(max) NULL, IpAddress nvarchar(50) NULL, UserAgent nvarchar(512) NULL);
            INSERT INTO dbo.ActivityLog (EventId,EventTimeUtc,ActorId,Operation,TargetName,MetaData)
                VALUES (205,'2026-01-01T00:00:00','legacy-one','Old create','Old target','{"legacy":1}'),
                       (301,NULL,'legacy-two','Old approval','Old comment','{"legacy":2}');
            """);
    }

    private static async Task AssertCanonicalSchemaAsync(CategoryTestDatabase database)
    {
        var postgres = database.Provider == "PostgreSql";
        var columns = await database.RowsAsync(postgres ? """
            SELECT column_name, data_type, is_nullable,
                   COALESCE(character_maximum_length::text,'<null>'),
                   COALESCE(datetime_precision::text,'<null>')
            FROM information_schema.columns WHERE table_schema='public' AND table_name='activity_log'
            ORDER BY column_name;
            """ : """
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE,
                   COALESCE(CONVERT(varchar(20),CHARACTER_MAXIMUM_LENGTH),'<null>'),
                   COALESCE(CONVERT(varchar(20),DATETIME_PRECISION),'<null>')
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='ActivityLog'
            ORDER BY COLUMN_NAME;
            """);
        string Text(int limit) => $"{(postgres ? "character varying" : "nvarchar")}|YES|{limit}|<null>";
        Dictionary<string, string> expected = postgres ? new()
        {
            ["id"] = "bigint|NO|<null>|<null>",
            ["event_key"] = "character varying|NO|100|<null>",
            ["event_time_utc"] = "timestamp with time zone|NO|<null>|6",
            ["actor_kind"] = "integer|NO|<null>|<null>",
            ["actor_display_name"] = Text(100),
            ["target_id"] = Text(100),
            ["target_display_name"] = Text(200),
            ["ip_address"] = Text(50),
            ["user_agent"] = Text(512),
            ["details"] = "text|YES|<null>|<null>"
        } : new()
        {
            ["Id"] = "bigint|NO|<null>|<null>",
            ["EventKey"] = "nvarchar|NO|100|<null>",
            ["EventTimeUtc"] = "datetime2|NO|<null>|7",
            ["ActorKind"] = "int|NO|<null>|<null>",
            ["ActorDisplayName"] = Text(100),
            ["TargetId"] = Text(100),
            ["TargetDisplayName"] = Text(200),
            ["IpAddress"] = Text(50),
            ["UserAgent"] = Text(512),
            ["Details"] = "nvarchar|YES|-1|<null>"
        };
        Assert.Equal(10, columns.Length);
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), columns.Select(row => row[0]).Order(StringComparer.Ordinal));
        foreach (var column in columns) Assert.Equal(expected[column[0]], string.Join("|", column.Skip(1)));
        var identity = await database.RowsAsync(postgres
            ? "SELECT is_identity FROM information_schema.columns WHERE table_schema='public' AND table_name='activity_log' AND column_name='id';"
            : "SELECT CONVERT(varchar(10),COLUMNPROPERTY(OBJECT_ID('dbo.ActivityLog'),'Id','IsIdentity'));");
        Assert.Equal(postgres ? "YES" : "1", Assert.Single(identity)[0]);
        var key = await database.RowsAsync(postgres ? """
            SELECT k.column_name FROM information_schema.table_constraints t
            JOIN information_schema.key_column_usage k USING (constraint_catalog,constraint_schema,constraint_name)
            WHERE t.table_schema='public' AND t.table_name='activity_log' AND t.constraint_type='PRIMARY KEY';
            """ : """
            SELECT k.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k ON t.CONSTRAINT_NAME=k.CONSTRAINT_NAME AND t.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA
            WHERE t.TABLE_SCHEMA='dbo' AND t.TABLE_NAME='ActivityLog' AND t.CONSTRAINT_TYPE='PRIMARY KEY';
            """);
        Assert.Equal(postgres ? "id" : "Id", Assert.Single(key)[0]);
    }
}
