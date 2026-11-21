using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Tests.TestSupport;
using Moonglade.Widgets;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace Moonglade.Web.Tests.Handlers;

public class WidgetCollectionTests
{
    [Fact]
    public async Task Replacement_CanonicalizesAndPreservesMetadata_AndRetryDoesNotDuplicateRowsOrAuditChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await Store.CreateAsync(ct);
        var first = Draft("First");
        var deleted = Draft("Deleted name");
        var original = await store.SaveAsync([first, deleted], ct);
        var timestamp = original.Collection.Widgets.Single(widget => widget.Id == first.Id).CreatedTimeUtc;
        var second = Draft("Second");
        var third = Draft("Third");
        first.Title = " Changed ";
        first.Region = WidgetRegion.BeforeTags;
        first.SortOrder = 999;
        second.SortOrder = third.SortOrder = 999;
        third.IsEnabled = false;

        var result = await store.SaveAsync([third, first, second], ct);

        Assert.Equal([first.Id, third.Id, second.Id], result.Collection.Widgets.Select(widget => widget.Id));
        Assert.Equal([0, 0, 1], result.Collection.Widgets.Select(widget => widget.SortOrder));
        var surviving = result.Collection.Widgets.Single(widget => widget.Id == first.Id);
        Assert.Equal(timestamp, surviving.CreatedTimeUtc);
        Assert.Equal("Changed", surviving.Title);
        Assert.False(result.Collection.Widgets.Single(widget => widget.Id == third.Id).IsEnabled);
        Assert.Equal(2, result.Changes.Count(change => change.Kind == WidgetChangeKind.Created));
        Assert.Equal("Deleted name", Assert.Single(result.Changes, change => change.Kind == WidgetChangeKind.Deleted).Title);
        Assert.Single(result.Changes, change => change.Kind == WidgetChangeKind.Updated);

        var retry = await store.SaveAsync([third, first, second], ct);
        Assert.Equal(result.Collection.Widgets, retry.Collection.Widgets);
        Assert.Empty(retry.Changes);
        Assert.Empty(store.Db.ChangeTracker.Entries<WidgetEntity>());
    }

    [Fact]
    public async Task LaterCompleteDraft_RemovesOtherTabsAdditions_AndRestoresDeletedIdsWithNewUtc()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await Store.CreateAsync(ct);
        var first = Draft("A");
        var restored = Draft("B");
        await store.SaveAsync([first, restored], ct);
        var oldTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await store.Db.Widget.Where(widget => widget.Id == restored.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(widget => widget.CreatedTimeUtc, oldTime), ct);
        var concurrentAddition = Draft("C");
        await store.SaveAsync([first, concurrentAddition], ct);

        var result = await store.SaveAsync([first, restored], ct);

        Assert.Equal([first.Id, restored.Id], result.Collection.Widgets.Select(widget => widget.Id));
        var recreated = result.Collection.Widgets.Single(widget => widget.Id == restored.Id);
        Assert.True(recreated.CreatedTimeUtc > oldTime);
        Assert.Equal(DateTimeKind.Utc, recreated.CreatedTimeUtc.Kind);
        Assert.Contains(result.Changes, change => change.Id == restored.Id && change.Kind == WidgetChangeKind.Created);
        Assert.Contains(result.Changes, change => change.Id == concurrentAddition.Id && change.Kind == WidgetChangeKind.Deleted);
        await store.SaveAsync([], ct);
        Assert.Empty(await store.Db.Widget.ToListAsync(ct));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("duplicate")]
    [InlineData("emptyId")]
    [InlineData("title")]
    [InlineData("content")]
    [InlineData("region")]
    [InlineData("type")]
    [InlineData("order")]
    [InlineData("item")]
    public async Task InvalidMixedDraft_DoesNotChangeAnyStoredWidget(string mode)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await Store.CreateAsync(ct);
        var baseline = await store.SaveAsync([Draft("Original")], ct);
        var valid = Draft("Valid");
        var invalid = Draft("Invalid");
        switch (mode)
        {
            case "duplicate": invalid.Id = valid.Id; break;
            case "emptyId": invalid.Id = Guid.Empty; break;
            case "title": invalid.Title = new string('A', 129); break;
            case "content": invalid.ContentCode = new string('A', 2001); break;
            case "region": invalid.Region = (WidgetRegion)99; break;
            case "type": invalid.WidgetType = (WidgetType)99; break;
            case "order": invalid.SortOrder = -1; break;
            case "item": invalid.ContentCode = "[{\"name\":\"Missing URL\"}]"; break;
        }

        await Assert.ThrowsAsync<ValidationException>(() => store.SaveAsync([valid, mode == "null" ? null : invalid], ct));

        Assert.Equal(baseline.Collection.Widgets,
            (await new ListWidgetsQueryHandler(store.Db, new WidgetCollectionGate(store.Db, new SqliteWidgetCollectionLock(store.Db))).HandleAsync(new ListWidgetsQuery(), ct)).Select(WidgetSnapshot.From));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureOrCancellationAfterSqlWrite_RollsBackAndLeavesContextReusable(bool cancel)
    {
        var ct = TestContext.Current.CancellationToken;
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var fault = new WriteFault();
        await using var store = await Store.CreateAsync(ct, fault);
        var baseline = await store.SaveAsync([Draft("Original")], ct);
        fault.Fail = !cancel;
        fault.Cancel = cancel ? canceled : null;

        await Assert.ThrowsAnyAsync<Exception>(() => store.SaveAsync([Draft("Replacement")], canceled.Token));

        Assert.Empty(store.Db.ChangeTracker.Entries<WidgetEntity>());
        Assert.Equal(baseline.Collection.Widgets,
            (await new ListWidgetsQueryHandler(store.Db, new WidgetCollectionGate(store.Db, new SqliteWidgetCollectionLock(store.Db))).HandleAsync(new ListWidgetsQuery(), ct)).Select(WidgetSnapshot.From));
        fault.Fail = false;
        fault.Cancel = null;
        Assert.Single((await store.SaveAsync([Draft("Next successful save")], ct)).Collection.Widgets);
    }

    [Fact]
    public async Task LostCommitAcknowledgment_RetryPreservesSavedIdentityUtcAndOriginalAuditSnapshots()
    {
        var ct = TestContext.Current.CancellationToken;
        var fault = new CommitFault();
        await using var store = await Store.CreateAsync(ct, fault, retry: true);
        var widget = Draft("Stable ID");
        fault.FailNext = true;

        var saved = await store.SaveAsync([widget], ct);

        Assert.True(fault.Failed);
        Assert.Equal(widget.Id, Assert.Single(saved.Collection.Widgets).Id);
        Assert.Equal(WidgetChangeKind.Created, Assert.Single(saved.Changes).Kind);
        Assert.Equal(saved.Collection.Widgets,
            (await new ListWidgetsQueryHandler(store.Db, new WidgetCollectionGate(store.Db, new SqliteWidgetCollectionLock(store.Db))).HandleAsync(new ListWidgetsQuery(), ct)).Select(WidgetSnapshot.From));
    }

    [Fact]
    public async Task CancellationAfterCommit_PreservesSuccessfulSaveResult()
    {
        var ct = TestContext.Current.CancellationToken;
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var fault = new CommitFault();
        await using var store = await Store.CreateAsync(ct, fault);
        fault.Cancel = canceled;

        var result = await store.SaveAsync([Draft("Committed")], canceled.Token);

        Assert.True(canceled.IsCancellationRequested);
        Assert.Equal(result.Collection.Widgets,
            (await new ListWidgetsQueryHandler(store.Db, new WidgetCollectionGate(store.Db, new SqliteWidgetCollectionLock(store.Db))).HandleAsync(new ListWidgetsQuery(), ct)).Select(WidgetSnapshot.From));
    }

    public static WidgetDraft Draft(string title) => new()
    {
        Id = Guid.NewGuid(), Title = title, WidgetType = WidgetType.LinkList,
        Region = WidgetRegion.AfterCategories, SortOrder = 0, IsEnabled = true, ContentCode = "[]"
    };

    [Fact]
    public async Task CollectionOperation_RejectsEnclosingTransactionOrPendingChangesWithoutDiscardingThem()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await Store.CreateAsync(ct);
        await using (var transaction = await store.Db.Database.BeginTransactionAsync(ct))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync([Draft("Nested")], ct));
            Assert.Same(transaction, store.Db.Database.CurrentTransaction);
            await transaction.RollbackAsync(ct);
        }
        var pending = new BlogConfigurationEntity { CfgKey = "Pending", CfgValue = "{}", LastModifiedTimeUtc = DateTime.UtcNow };
        store.Db.BlogConfiguration.Add(pending);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync([Draft("Unrelated unit of work")], ct));
        Assert.Equal(EntityState.Added, store.Db.Entry(pending).State);
        Assert.Empty(await store.Db.Widget.AsNoTracking().ToListAsync(ct));
    }

    private sealed class Store(SqliteConnection connection, BlogDbContext db) : IAsyncDisposable
    {
        public BlogDbContext Db => db;
        public Task<WidgetCollectionSaveResult> SaveAsync(List<WidgetDraft?> widgets, CancellationToken ct) =>
            new SaveWidgetCollectionCommandHandler(db, new WidgetCollectionGate(db, new SqliteWidgetCollectionLock(db)))
                .HandleAsync(new SaveWidgetCollectionCommand(new WidgetCollectionRequest { Widgets = widgets }), ct);

        public static async Task<Store> CreateAsync(CancellationToken ct, IInterceptor? interceptor = null, bool retry = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(ct);
            var options = new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection);
            if (interceptor is not null) options.AddInterceptors(interceptor);
            if (retry) options.ReplaceService<IExecutionStrategyFactory, RetryFactory>();
            var db = new BlogDbContext(options.Options);
            await db.Database.EnsureCreatedAsync(ct);
            return new Store(connection, db);
        }

        public async ValueTask DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class WriteFault : SaveChangesInterceptor
    {
        public bool Fail { get; set; }
        public CancellationTokenSource? Cancel { get; set; }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
        {
            Cancel?.Cancel();
            if (Fail) throw new InvalidOperationException("Synthetic post-write failure.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CommitFault : DbTransactionInterceptor
    {
        public bool FailNext { get; set; }
        public bool Failed { get; private set; }
        public CancellationTokenSource? Cancel { get; set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken ct = default)
        {
            Cancel?.Cancel();
            if (FailNext) { FailNext = false; Failed = true; throw new TimeoutException("Synthetic lost acknowledgment."); }
            return Task.CompletedTask;
        }
    }

    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryStrategy(dependencies);
    }

    private sealed class RetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TimeoutException;
    }
}
