using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Tests.TestSupport;
using System.Reflection;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public partial class ActivityLogModelTests
{
    [Fact]
    public void Catalog_AllActiveEvents_HaveExactUniqueKeysGroupsAndLabels()
    {
        (string Key, string Group, string Label)[] expected =
        [
            ("category.created", "Categories", "Category created"),
            ("category.updated", "Categories", "Category updated"),
            ("category.deleted", "Categories", "Category deleted"),
            ("post.schedule.cancelled", "Posts", "Post schedule cancelled"),
            ("post.schedule.postponed", "Posts", "Post schedule postponed"),
            ("post.restored", "Posts", "Post restored"),
            ("post.permanently-deleted", "Posts", "Post permanently deleted"),
            ("post.recycle-bin.cleared", "Posts", "Post recycle bin cleared"),
            ("post.created", "Posts", "Post created"),
            ("post.updated", "Posts", "Post updated"),
            ("post.deleted", "Posts", "Post moved to recycle bin"),
            ("post.published", "Posts", "Post published"),
            ("post.automatically-published", "Posts", "Post automatically published"),
            ("post.unpublished", "Posts", "Post unpublished"),
            ("comment.approval-toggled", "Comments", "Comment approval toggled"),
            ("comment.deleted", "Comments", "Comments deleted"),
            ("comment.replied", "Comments", "Comment replied to"),
            ("page.created", "Pages", "Page created"),
            ("page.updated", "Pages", "Page updated"),
            ("page.deleted", "Pages", "Page moved to recycle bin"),
            ("page.restored", "Pages", "Page restored"),
            ("page.permanently-deleted", "Pages", "Page permanently deleted"),
            ("page.recycle-bin.cleared", "Pages", "Page recycle bin cleared"),
            ("tag.created", "Tags", "Tag created"),
            ("tag.updated", "Tags", "Tag updated"),
            ("tag.deleted", "Tags", "Tag deleted"),
            ("theme.created", "Themes", "Theme created"),
            ("theme.deleted", "Themes", "Theme deleted"),
            ("settings.general.updated", "Settings", "General settings updated"),
            ("settings.content.updated", "Settings", "Content settings updated"),
            ("settings.comments.updated", "Settings", "Comment settings updated"),
            ("settings.notifications.updated", "Settings", "Notification settings updated"),
            ("settings.subscription.updated", "Settings", "Subscription settings updated"),
            ("settings.images.updated", "Settings", "Image settings updated"),
            ("settings.advanced.updated", "Settings", "Advanced settings updated"),
            ("settings.seo.updated", "Settings", "SEO settings updated"),
            ("settings.appearance.updated", "Settings", "Appearance settings updated"),
            ("settings.custom-menu.updated", "Settings", "Custom menu settings updated"),
            ("account.profile.updated", "Account", "Account profile updated"),
            ("account.password.updated", "Account", "Account password updated"),
            ("account.authenticator.reset", "Account", "Account authenticator reset"),
            ("image.uploaded", "Images", "Image uploaded"),
            ("asset.avatar.updated", "Site assets", "Avatar updated"),
            ("asset.site-icon.updated", "Site assets", "Site icon updated"),
            ("asset.verification-file.created", "Site assets", "Verification file created"),
            ("asset.verification-file.updated", "Site assets", "Verification file updated"),
            ("asset.verification-file.deleted", "Site assets", "Verification file deleted"),
            ("asset.verification-file.toggled", "Site assets", "Verification file toggled"),
            ("widget.created", "Widgets", "Widget created"),
            ("widget.updated", "Widgets", "Widget updated"),
            ("widget.deleted", "Widgets", "Widget deleted"),
            ("activity-log.cleared", "Activity log", "Activity log cleared")
        ];
        var actual = ActivityEvents.All;
        Assert.Equal(52, actual.Count);
        Assert.Equal(expected.OrderBy(item => item.Key), actual.Select(item => (item.Key, item.Group, item.Label)).OrderBy(item => item.Key));
        Assert.Equal(52, actual.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(52, actual.Select(item => item.Label).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(expected.Select(item => item.Key).Order(), typeof(ActivityEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral)
            .Select(field => Assert.IsType<string>(field.GetRawConstantValue())).Order());
        Assert.Equal(["CategoryId", "Slug"], ActivityEvents.Get("category.created").DetailKeys.Order().ToArray());
        Assert.Equal(["CommentId"], ActivityEvents.Get("comment.replied").DetailKeys.ToArray());
        Assert.Equal(["PostId"], ActivityEvents.Get("post.automatically-published").DetailKeys.ToArray());
        Assert.Equal(["DeletedCount"], ActivityEvents.Get("activity-log.cleared").DetailKeys.ToArray());
        Assert.Equal(["Changes"], ActivityEvents.Get("settings.comments.updated").DetailKeys.ToArray());
    }

    [Fact]
    public async Task Writer_SqliteRoundTrip_PreservesGeneratedIdUtcSnapshotsAndTypedDetails()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var before = DateTime.UtcNow;
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostCreated,
            ActivityActorKind.Administrator, "  admin  ", id.ToString(), "  Captured <title>  ",
            ActivityLogDetails.Create(("PostId", id), ("Slug", "captured-slug"), ("PostStatus", PostStatus.Draft)),
            "192.0.2.40", "synthetic-agent"), ct);
        var saved = Assert.Single(await store.ReadLogsAsync(ct));
        Assert.True(saved.Id > 0);
        Assert.Equal("post.created", saved.EventKey);
        Assert.Equal(DateTimeKind.Utc, saved.EventTimeUtc.Kind);
        Assert.InRange(saved.EventTimeUtc, before, DateTime.UtcNow);
        var result = await new GetMetaDataByActivityLogIdQueryHandler(db)
            .HandleAsync(new GetMetaDataByActivityLogIdQuery(saved.Id), ct);
        Assert.NotNull(result);
        Assert.Equal(saved.Id, result.Id);
        Assert.Equal("admin", result.ActorDisplayName);
        Assert.Equal(ActivityActorKind.Administrator, result.ActorKind);
        Assert.Equal(id.ToString(), result.TargetId);
        Assert.Equal("Captured <title>", result.TargetDisplayName);
        Assert.Equal("192.0.2.40", result.IpAddress);
        Assert.Equal("synthetic-agent", result.UserAgent);
        Assert.Equal(id, result.Details["PostId"].GetGuid());
        Assert.Equal("captured-slug", result.Details["Slug"].GetString());
        Assert.Equal("Draft", result.Details["PostStatus"].GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Writer_SafeScalarAndFlatArrayValues_RoundTripWithoutLosingJsonTypes(int variant)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        object? value = variant switch
        {
            0 => null,
            1 => false,
            2 => 9876543210L,
            3 => 12.75m,
            4 => "<plain text>",
            _ => new object?[] { "first", 7, true, null, PostStatus.Published }
        };
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostCreated,
            ActivityActorKind.Administrator, "admin", Details: ActivityLogDetails.Create(("PostStatus", value))), ct);
        var saved = Assert.Single(await store.ReadLogsAsync(ct));
        var log = await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(saved.Id), ct);
        var actual = log!.Details["PostStatus"];
        switch (variant)
        {
            case 0: Assert.Equal(JsonValueKind.Null, actual.ValueKind); break;
            case 1: Assert.False(actual.GetBoolean()); break;
            case 2: Assert.Equal(9876543210L, actual.GetInt64()); break;
            case 3: Assert.Equal(12.75m, actual.GetDecimal()); break;
            case 4: Assert.Equal("<plain text>", actual.GetString()); break;
            default:
                var array = actual.EnumerateArray().ToArray();
                Assert.Equal(5, array.Length);
                Assert.Equal("first", array[0].GetString());
                Assert.Equal(7, array[1].GetInt32());
                Assert.True(array[2].GetBoolean());
                Assert.Equal(JsonValueKind.Null, array[3].ValueKind);
                Assert.Equal("Published", array[4].GetString());
                break;
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Writer_StringLimits_PreserveAtBoundaryAndTruncateOnlyExcessAfterTrim(int offset)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        string Input(char marker, int limit) => "  " + new string(marker, limit + offset) + "  ";
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostCreated,
            ActivityActorKind.Administrator, Input('a', 100), Input('i', 100), Input('t', 200),
            IpAddress: Input('p', 50), UserAgent: Input('u', 512)), ct);
        var row = Assert.Single(await store.ReadLogsAsync(ct));
        Assert.Equal(new string('a', Math.Min(100, 100 + offset)), row.ActorDisplayName);
        Assert.Equal(new string('i', Math.Min(100, 100 + offset)), row.TargetId);
        Assert.Equal(new string('t', Math.Min(200, 200 + offset)), row.TargetDisplayName);
        Assert.Equal(new string('p', Math.Min(50, 50 + offset)), row.IpAddress);
        Assert.Equal(new string('u', Math.Min(512, 512 + offset)), row.UserAgent);
        Assert.Equal("post.created", row.EventKey);
    }

    [Theory]
    [InlineData("unknown-event")]
    [InlineData("invalid-actor")]
    [InlineData("password")]
    [InlineData("category-note")]
    [InlineData("reply-body")]
    [InlineData("verification-bytes")]
    [InlineData("nested-array")]
    [InlineData("object-value")]
    public async Task Writer_InvalidEventsActorsAndRestrictedDetails_RejectBeforePersistenceAndLogNoContents(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        const string sentinel = "B2-PROTECTED-CONTENT-SENTINEL";
        var request = scenario switch
        {
            "unknown-event" => new CreateActivityLogCommand(sentinel, ActivityActorKind.Administrator, "admin"),
            "invalid-actor" => new CreateActivityLogCommand(ActivityEvents.PostCreated, (ActivityActorKind)99, sentinel),
            "password" => new CreateActivityLogCommand(ActivityEvents.SettingsPasswordUpdated, ActivityActorKind.Administrator, "admin",
                Details: ActivityLogDetails.Create(("Password", sentinel))),
            "category-note" => new CreateActivityLogCommand(ActivityEvents.CategoryCreated, ActivityActorKind.Administrator, "admin",
                Details: ActivityLogDetails.Create(("Note", sentinel))),
            "reply-body" => new CreateActivityLogCommand(ActivityEvents.CommentReplied, ActivityActorKind.Administrator, "admin",
                Details: ActivityLogDetails.Create(("ReplyContent", sentinel))),
            "verification-bytes" => new CreateActivityLogCommand(ActivityEvents.SiteVerificationFileCreated, ActivityActorKind.Administrator, "admin",
                Details: ActivityLogDetails.Create(("ContentBytes", new byte[] { 1, 2, 3 }))),
            "nested-array" => new CreateActivityLogCommand(ActivityEvents.CommentDeleted, ActivityActorKind.Administrator, "admin",
                Details: ActivityLogDetails.Create(("CommentIds", new object[] { new[] { sentinel } }))),
            _ => new CreateActivityLogCommand(ActivityEvents.PostCreated, ActivityActorKind.Administrator, "admin",
                Details: ActivityLogDetails.Create(("PostId", new Dictionary<string, string> { ["secret"] = sentinel })))
        };
        var unrelated = new BlogConfigurationEntity { CfgKey = "B2Synthetic", CfgValue = "retained", LastModifiedTimeUtc = DateTime.UtcNow };
        db.Add(unrelated);
        await Writer(db, store).HandleAsync(request, ct);
        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Empty(await db.BlogConfiguration.AsNoTracking().ToListAsync(ct));
        Assert.Equal(EntityState.Added, db.Entry(unrelated).State);
        Assert.Empty(db.ChangeTracker.Entries<ActivityLogEntity>());
        var diagnostic = Assert.Single(store.WriterLogger.Invocations);
        Assert.Equal(LogLevel.Error, diagnostic.Arguments[0]);
        Assert.Null(diagnostic.Arguments[3]);
        var message = diagnostic.Arguments[2]!.ToString()!;
        Assert.Contains("ArgumentException", message);
        Assert.DoesNotContain(sentinel, message);
        Assert.DoesNotContain("Password", message);
        await db.SaveChangesAsync(ct);
        Assert.Equal("retained", (await db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
        Assert.Empty(await store.ReadLogsAsync(ct));
    }

    [Fact]
    public async Task WriterAndReader_SystemOmitsRequestContext_AdministratorNamedSystemRemainsAdministrator()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        foreach (var kind in new[] { ActivityActorKind.System, ActivityActorKind.Administrator })
            await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostPublished, kind,
                "System", "post-id", "Captured title", IpAddress: "192.0.2.45", UserAgent: "private-agent"), ct);
        var rows = await store.ReadLogsAsync(ct);
        var system = Assert.Single(rows, row => row.ActorKind == ActivityActorKind.System);
        Assert.Null(system.ActorDisplayName);
        Assert.Null(system.IpAddress);
        Assert.Null(system.UserAgent);
        var administrator = Assert.Single(rows, row => row.ActorKind == ActivityActorKind.Administrator);
        Assert.Equal("System", administrator.ActorDisplayName);
        Assert.Equal("192.0.2.45", administrator.IpAddress);
        Assert.Equal("private-agent", administrator.UserAgent);
        var trackedSystem = await db.ActivityLog.SingleAsync(row => row.Id == system.Id, ct);
        trackedSystem.ActorDisplayName = "must-not-render";
        trackedSystem.IpAddress = "192.0.2.99";
        trackedSystem.UserAgent = "must-not-render";
        await db.SaveChangesAsync(ct);
        var read = await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(system.Id), ct);
        Assert.Equal(ActivityActorKind.System, read!.ActorKind);
        Assert.Null(read.ActorDisplayName);
        Assert.Null(read.IpAddress);
        Assert.Null(read.UserAgent);
        Assert.Equal("post-id", read.TargetId);
        Assert.Equal("Captured title", read.TargetDisplayName);
    }

    [Fact]
    public async Task List_TimeThenIdDescending_PaginatesInclusiveDatesAndRepeatedKeyFilters()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var start = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);
        ActivityLogEntity Row(DateTime time, string key) => new()
        {
            EventKey = key,
            EventTimeUtc = time,
            ActorKind = ActivityActorKind.Administrator,
            ActorDisplayName = "admin",
            TargetDisplayName = "Captured"
        };
        ActivityLogEntity[] rows =
        [
            Row(start.AddTicks(-1), ActivityEvents.PostCreated),
            Row(start, ActivityEvents.PostCreated),
            Row(start.AddMinutes(30), ActivityEvents.CommentDeleted),
            Row(start.AddMinutes(30), ActivityEvents.PostCreated),
            Row(end, ActivityEvents.PostCreated),
            Row(end.AddTicks(1), ActivityEvents.PostCreated)
        ];
        db.AddRange(rows);
        await db.SaveChangesAsync(ct);
        var handler = new ListActivityLogsQueryHandler(db);
        var all = await handler.HandleAsync(new(10, 1), ct);
        Assert.Equal(6, all.TotalCount);
        Assert.Equal(new[] { rows[5].Id, rows[4].Id, rows[3].Id, rows[2].Id, rows[1].Id, rows[0].Id }, all.Logs.Select(row => row.Id));
        var emptyKeys = await handler.HandleAsync(new(EventKeys: []), ct);
        Assert.Equal(all.Logs.Select(row => row.Id), emptyKeys.Logs.Select(row => row.Id));
        var startOnly = await handler.HandleAsync(new(StartTimeUtc: start), ct);
        Assert.Equal(5, startOnly.TotalCount);
        Assert.Equal(new[] { rows[5].Id, rows[4].Id, rows[3].Id, rows[2].Id, rows[1].Id }, startOnly.Logs.Select(row => row.Id));
        var endOnly = await handler.HandleAsync(new(EndTimeUtc: end), ct);
        Assert.Equal(5, endOnly.TotalCount);
        Assert.Equal(new[] { rows[4].Id, rows[3].Id, rows[2].Id, rows[1].Id, rows[0].Id }, endOnly.Logs.Select(row => row.Id));
        var first = await handler.HandleAsync(new(2, 1, [ActivityEvents.PostCreated, ActivityEvents.CommentDeleted, ActivityEvents.PostCreated], start, end), ct);
        var second = await handler.HandleAsync(new(2, 2, [ActivityEvents.PostCreated, ActivityEvents.CommentDeleted], start, end), ct);
        Assert.Equal(4, first.TotalCount);
        Assert.Equal(4, second.TotalCount);
        Assert.Equal(new[] { rows[4].Id, rows[3].Id }, first.Logs.Select(row => row.Id));
        Assert.Equal(new[] { rows[2].Id, rows[1].Id }, second.Logs.Select(row => row.Id));
        var exact = await handler.HandleAsync(new(10, 1, [ActivityEvents.PostCreated], start, start), ct);
        Assert.Equal(rows[1].Id, Assert.Single(exact.Logs).Id);
        Assert.Equal(1, exact.TotalCount);
        var beyond = await handler.HandleAsync(new(2, 3, null, start, end), ct);
        Assert.Empty(beyond.Logs);
        Assert.Equal(4, beyond.TotalCount);
        var noMatch = await handler.HandleAsync(new(10, 1, [ActivityEvents.TagCreated]), ct);
        Assert.Empty(noMatch.Logs);
        Assert.Equal(0, noMatch.TotalCount);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("205")]
    [InlineData("POST.CREATED")]
    public async Task List_UnsupportedOrNumericKeys_RejectRatherThanSilentlyDroppingFilters(string key)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(1, ct);
        using var scope = store.Services.CreateScope();
        var handler = new ListActivityLogsQueryHandler(scope.ServiceProvider.GetRequiredService<BlogDbContext>());
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new(EventKeys: [ActivityEvents.PostCreated, key]), ct));
        Assert.Contains("Unsupported activity event filter", exception.Message);
        Assert.Single(await store.ReadLogsAsync(ct));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    public async Task List_NonpositivePaging_RejectsBeforeReading(int pageSize, int pageIndex)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new ListActivityLogsQueryHandler(scope.ServiceProvider.GetRequiredService<BlogDbContext>())
                .HandleAsync(new(pageSize, pageIndex), ct));
    }

    [Fact]
    public async Task Detail_TargetSnapshotSurvivesCategoryDeletion_EmptyDetailsStillReturnRow()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var category = new CategoryEntity { DisplayName = "Original category", Slug = "original", Note = "Not copied" };
        db.Add(category);
        await db.SaveChangesAsync(ct);
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.CategoryDeleted,
            ActivityActorKind.Administrator, "admin", category.Id.ToString(), category.DisplayName), ct);
        db.Remove(category);
        await db.SaveChangesAsync(ct);
        Assert.Empty(await db.Category.ToListAsync(ct));
        var saved = Assert.Single(await store.ReadLogsAsync(ct));
        var handler = new GetMetaDataByActivityLogIdQueryHandler(db);
        var detail = await handler.HandleAsync(new(saved.Id), ct);
        Assert.NotNull(detail);
        Assert.Equal(category.Id.ToString(), detail.TargetId);
        Assert.Equal("Original category", detail.TargetDisplayName);
        Assert.Empty(detail.Details);
        Assert.Null(await handler.HandleAsync(new(saved.Id + 1), ct));
    }

    [Theory]
    [InlineData("{\"Password\":\"B2-STORED-PROTECTED-SENTINEL\"}")]
    [InlineData("{\"PostId\":{\"secret\":\"B2-STORED-PROTECTED-SENTINEL\"}}")]
    [InlineData("{\"PostId\":[[\"B2-STORED-PROTECTED-SENTINEL\"]]}")]
    [InlineData("null")]
    [InlineData("{")]
    public async Task Detail_InvalidStoredFieldsOrShapes_FailsExplicitlyWithoutEchoingPayload(string details)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var row = new ActivityLogEntity
        {
            EventKey = ActivityEvents.PostCreated,
            EventTimeUtc = DateTime.UtcNow,
            ActorKind = ActivityActorKind.Administrator,
            Details = details
        };
        db.Add(row);
        await db.SaveChangesAsync(ct);
        var exception = await Record.ExceptionAsync(() => new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(row.Id), ct));
        Assert.True(exception is InvalidDataException or JsonException);
        Assert.DoesNotContain("B2-STORED-PROTECTED-SENTINEL", exception!.Message);
        var listFailure = await Record.ExceptionAsync(() => new ListActivityLogsQueryHandler(db).HandleAsync(new(), ct));
        Assert.True(listFailure is InvalidDataException or JsonException);
        Assert.DoesNotContain("B2-STORED-PROTECTED-SENTINEL", listFailure!.Message);
    }

    [Theory]
    [InlineData("guids")]
    [InlineData("integers")]
    [InlineData("empty")]
    [InlineData("singleton")]
    public async Task Writer_FlatGuidAndIntegerArrays_PreserveTypeOrderAndEmptyPartition(string variant)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Array value = variant switch
        {
            "guids" => new[] { second, first },
            "integers" => new[] { 99, 1, 7 },
            "singleton" => new[] { 42 },
            _ => Array.Empty<int>()
        };
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.CommentDeleted,
            ActivityActorKind.Administrator, "admin", Details: ActivityLogDetails.Create(("CommentIds", value))), ct);
        var saved = Assert.Single(await store.ReadLogsAsync(ct));
        var detail = await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(saved.Id), ct);
        var items = detail!.Details["CommentIds"].EnumerateArray().ToArray();
        if (variant == "guids") Assert.Equal(new[] { second, first }, items.Select(item => item.GetGuid()));
        else Assert.Equal(variant == "integers" ? [99, 1, 7] : variant == "singleton" ? [42] : Array.Empty<int>(),
            items.Select(item => item.GetInt32()));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public async Task Writer_NullEmptyAndWhitespaceOptionalFields_PreserveIntentionalEmptyDetails(string? input, string? expected)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostCreated,
            ActivityActorKind.Administrator, input, input, input, new Dictionary<string, object?>(), input, input), ct);
        var saved = Assert.Single(await store.ReadLogsAsync(ct));
        Assert.Equal(expected, saved.ActorDisplayName);
        Assert.Equal(expected, saved.TargetId);
        Assert.Equal(expected, saved.TargetDisplayName);
        Assert.Equal(expected, saved.IpAddress);
        Assert.Equal(expected, saved.UserAgent);
        Assert.Null(saved.Details);
        var detail = await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(saved.Id), ct);
        Assert.Empty(detail!.Details);
        Assert.Equal("post.created", detail.EventKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Writer_UndefinedActorKinds_DoNotPersistOrSaveUnrelatedWork(int kind)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostCreated, (ActivityActorKind)kind, "admin"), ct);
        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Contains("ArgumentException", Assert.Single(store.WriterLogger.Invocations).Arguments[2]!.ToString());
    }

    private static CreateActivityLogCommandHandler Writer(BlogDbContext db, ActivityLogTestStore store) =>
        new(db, store.WriterLogger.Object);
}
