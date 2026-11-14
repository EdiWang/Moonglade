using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Tests.TestSupport;

namespace Moonglade.Web.Tests.Controllers;

public partial class ActivityLogModelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Writer_PublicationOccurrence_PreservesSuppliedUtcOrDefaultsToCurrentUtc(bool supplied)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var occurrence = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1234);
        var id = Guid.NewGuid();
        var before = DateTime.UtcNow;

        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostAutomaticallyPublished,
            ActivityActorKind.System, null, id.ToString(), "Published snapshot",
            ActivityLogDetails.Create(("PostId", id)), EventTimeUtc: supplied ? occurrence : null), ct);

        var row = Assert.Single(await store.ReadLogsAsync(ct));
        Assert.Equal(DateTimeKind.Utc, row.EventTimeUtc.Kind);
        if (supplied) Assert.Equal(occurrence, row.EventTimeUtc);
        else Assert.InRange(row.EventTimeUtc, before, DateTime.UtcNow);
        var detail = await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(row.Id), ct);
        Assert.Equal(row.EventTimeUtc, detail!.EventTimeUtc);
        Assert.Equal("post.automatically-published", detail.EventKey);
        Assert.Equal("Published snapshot", detail.TargetDisplayName);
        Assert.Equal(id, Assert.Single(detail.Details).Value.GetGuid());
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task Writer_NonUtcSuppliedOccurrence_RejectsSafelyAndDoesNotResurrectOnLaterSave(DateTimeKind kind)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        const string secret = "B4-PROTECTED-TITLE-SENTINEL";
        var pending = new BlogConfigurationEntity { CfgKey = "B4-pending", CfgValue = "retained", LastModifiedTimeUtc = DateTime.UtcNow };
        db.Add(pending);

        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostAutomaticallyPublished,
            ActivityActorKind.System, null, Guid.NewGuid().ToString(), secret,
            EventTimeUtc: new DateTime(2026, 1, 1, 0, 0, 0, kind)), ct);

        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<ActivityLogEntity>());
        Assert.Equal(EntityState.Added, db.Entry(pending).State);
        Assert.Empty(await db.BlogConfiguration.AsNoTracking().ToListAsync(ct));
        var diagnostic = Assert.Single(store.WriterLogger.Invocations, invocation => invocation.Method.Name == "Log");
        Assert.Equal(LogLevel.Error, diagnostic.Arguments[0]);
        Assert.Null(diagnostic.Arguments[3]);
        var text = diagnostic.Arguments[2]!.ToString()!;
        Assert.Contains(nameof(InvalidOperationException), text);
        Assert.DoesNotContain(secret, text);
        Assert.DoesNotContain("must contain a UTC", text);
        await db.SaveChangesAsync(ct);
        Assert.Equal("retained", (await db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
        Assert.Empty(await store.ReadLogsAsync(ct));
    }

    [Theory]
    [InlineData("insert-failure", false)]
    [InlineData("insert-failure", true)]
    [InlineData("caller-cancellation", false)]
    [InlineData("caller-cancellation", true)]
    public async Task Writer_SuppliedUtcWriteFailsOrCancels_DetachesActivityAndLogsOnlySafeFailureType(string scenario, bool includeTrace)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        const string secret = "B4-PRIVATE-WRITER-TITLE-AND-EXCEPTION";
        const string trace = "b2-writer-trace";
        var pending = new BlogConfigurationEntity { CfgKey = "B4-failed-writer", CfgValue = "retained", LastModifiedTimeUtc = DateTime.UtcNow };
        db.Add(pending);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (scenario == "caller-cancellation") cancellation.Cancel();
        else
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER b4_reject_activity BEFORE INSERT ON ActivityLog BEGIN SELECT RAISE(ABORT, 'B4-PRIVATE-WRITER-TITLE-AND-EXCEPTION'); END;", ct);

        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.PostAutomaticallyPublished,
            ActivityActorKind.System, null, Guid.NewGuid().ToString(), secret,
            EventTimeUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        {
            TraceIdentifier = includeTrace ? trace : null
        }, cancellation.Token);

        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<ActivityLogEntity>());
        Assert.Equal(EntityState.Added, db.Entry(pending).State);
        var diagnostic = Assert.Single(store.WriterLogger.Invocations, invocation => invocation.Method.Name == "Log");
        Assert.Null(diagnostic.Arguments[3]);
        Assert.Equal(LogLevel.Error, diagnostic.Arguments[0]);
        var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(diagnostic.Arguments[2]);
        Assert.Equal(includeTrace ? ["EventKey", "FailureType", "TraceIdentifier", "{OriginalFormat}"] :
            new[] { "EventKey", "FailureType", "{OriginalFormat}" }, fields.Select(field => field.Key).ToArray());
        Assert.Contains(fields, field => field.Key == "EventKey" && Equals(field.Value, "post.automatically-published"));
        if (includeTrace)
            Assert.Contains(fields, field => field.Key == "TraceIdentifier" && Equals(field.Value, trace));
        else
            Assert.Contains(fields, field => field.Key == "{OriginalFormat}" &&
                Equals(field.Value, "Activity logging failed for {EventKey}. Failure type: {FailureType}"));
        var failureType = Assert.IsType<string>(fields.Single(field => field.Key == "FailureType").Value);
        if (scenario == "insert-failure") Assert.Equal(nameof(DbUpdateException), failureType);
        else Assert.Contains(failureType, new[] { nameof(OperationCanceledException), nameof(TaskCanceledException) });
        Assert.DoesNotContain(secret, diagnostic.Arguments[2]!.ToString()!);
        if (scenario == "insert-failure") await db.Database.ExecuteSqlRawAsync("DROP TRIGGER b4_reject_activity;", ct);
        await db.SaveChangesAsync(ct);
        Assert.Equal("retained", (await db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
        Assert.Empty(await store.ReadLogsAsync(ct));
    }
}
