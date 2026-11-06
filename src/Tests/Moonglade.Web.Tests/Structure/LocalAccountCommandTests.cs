using LiteBus.Events.Abstractions;
using LiteBus.Messaging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Tests.Structure;

public class LocalAccountCommandTests
{
    [Fact]
    public async Task Save_PublishesDetachedCredentialsBeforeLegacyEvent_AndNoOpPreservesTimestamp()
    {
        var ct = TestContext.Current.CancellationToken;
        using var store = new ConfigurationCommandRecorder();
        store.Initialize(new BlogConfig());
        var events = new Mock<IEventMediator>();
        var account = store.Accounts.Read();
        account.Username = "updated-admin";
        var command = new UpdateLocalAccountCommand(account);
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Callback((IEvent change, EventMediationSettings settings, CancellationToken token) =>
            {
                Assert.Equal(command.Json, store.Accounts.Read().ToJson());
                Assert.Equal(command.Json, Stored(store));
                Assert.Equal(CancellationToken.None, token);
            }).Returns(Task.CompletedTask);

        await Handler(store, events.Object).HandleAsync(command, ct);
        var timestamp = await Timestamp(store);
        account.Username = "mutated-input";
        store.Accounts.Read().Username = "mutated-output";
        await Handler(store, events.Object).HandleAsync(command, ct);

        Assert.Equal(command.Json, store.Accounts.Read().ToJson());
        Assert.Equal(timestamp, await Timestamp(store));
        var change = Assert.IsType<ConfigurationChangedEvent>(Assert.Single(events.Invocations).Arguments[0]);
        Assert.Equal(new[] { nameof(LocalAccountSettings) }, change.Names);
    }

    [Theory]
    [InlineData("username")]
    [InlineData("hash")]
    [InlineData("salt")]
    [InlineData("missing-secret")]
    [InlineData("invalid-secret")]
    public async Task InvalidAccount_DoesNotWritePublishOrNotify(string invalidField)
    {
        using var store = new ConfigurationCommandRecorder();
        store.Initialize(new BlogConfig());
        var original = Stored(store);
        var timestamp = await Timestamp(store);
        var account = store.Accounts.Read();
        switch (invalidField)
        {
            case "username": account.Username = ""; break;
            case "hash": account.PasswordHash = ""; break;
            case "salt": account.PasswordSalt = ""; break;
            case "missing-secret": account.IsTotpEnabled = true; break;
            case "invalid-secret": account.TotpSecret = "private-marker!"; break;
        }
        var events = new Mock<IEventMediator>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Handler(store, events.Object)
            .HandleAsync(new UpdateLocalAccountCommand(account), TestContext.Current.CancellationToken));

        Assert.DoesNotContain("private-marker", exception.ToString());
        Assert.Equal(original, Stored(store));
        Assert.Equal(original, store.Accounts.Read().ToJson());
        Assert.Equal(timestamp, await Timestamp(store));
        Assert.Empty(events.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingRowOrFailedSave_PreservesRuntimeAndDoesNotNotify(bool missingRow)
    {
        var ct = TestContext.Current.CancellationToken;
        using var store = new ConfigurationCommandRecorder();
        store.Initialize(new BlogConfig());
        var original = store.Accounts.Read().ToJson();
        if (missingRow)
            await store.Db.BlogConfiguration.Where(row => row.CfgKey == nameof(LocalAccountSettings)).ExecuteDeleteAsync(ct);
        else
            await store.Db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_account BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;", ct);
        var account = store.Accounts.Read();
        account.Username = "changed";
        var events = new Mock<IEventMediator>();

        var save = () => Handler(store, events.Object).HandleAsync(new UpdateLocalAccountCommand(account), ct);
        if (missingRow) await Assert.ThrowsAsync<InvalidOperationException>(save);
        else await Assert.ThrowsAsync<DbUpdateException>(save);

        Assert.Equal(original, store.Accounts.Read().ToJson());
        if (!missingRow) Assert.Equal(original, Stored(store));
        Assert.Empty(events.Invocations);
    }

    [Fact]
    public async Task SharedWriteGate_BlocksAccountUntilReleased_AndCancellationDoesNotPublish()
    {
        var ct = TestContext.Current.CancellationToken;
        using var store = new ConfigurationCommandRecorder();
        store.Initialize(new BlogConfig());
        var original = store.Accounts.Read().ToJson();
        var account = store.Accounts.Read();
        account.Username = "changed";
        var events = new Mock<IEventMediator>();
        await store.Gate.WaitAsync(ct);
        using var cancelled = new CancellationTokenSource();
        var pending = Handler(store, events.Object).HandleAsync(new UpdateLocalAccountCommand(account), cancelled.Token);
        Assert.False(pending.IsCompleted);
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(original, Stored(store));
        Assert.Equal(original, store.Accounts.Read().ToJson());
        Assert.Empty(events.Invocations);
        store.Gate.Release();

        await Handler(store, events.Object).HandleAsync(new UpdateLocalAccountCommand(account), ct);

        Assert.Equal(account.ToJson(), store.Accounts.Read().ToJson());
    }

    private static UpdateLocalAccountCommandHandler Handler(ConfigurationCommandRecorder store, IEventMediator events) =>
        new(store.Db, store.Accounts, store.Gate, events, NullLogger<UpdateLocalAccountCommandHandler>.Instance);

    private static string Stored(ConfigurationCommandRecorder store) => store.Db.BlogConfiguration.AsNoTracking()
        .Single(row => row.CfgKey == nameof(LocalAccountSettings)).CfgValue;

    [Fact]
    public async Task CancellationAfterCommit_StillPublishesAndNotifies()
    {
        using var store = new ConfigurationCommandRecorder();
        store.Initialize(new BlogConfig());
        using var cancellation = new CancellationTokenSource();
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(store.Db.Database.GetDbConnection()).AddInterceptors(new CancelAfterSave(cancellation)).Options);
        var account = store.Accounts.Read();
        account.Username = "committed-before-disconnect";
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Callback(() =>
            {
                Assert.True(cancellation.IsCancellationRequested);
                Assert.Equal(account.ToJson(), store.Accounts.Read().ToJson());
                Assert.Equal(account.ToJson(), Stored(store));
            }).Returns(Task.CompletedTask);

        await new UpdateLocalAccountCommandHandler(db, store.Accounts, store.Gate, events.Object,
            NullLogger<UpdateLocalAccountCommandHandler>.Instance).HandleAsync(new UpdateLocalAccountCommand(account), cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Single(events.Invocations);
    }

    [Fact]
    public async Task ConcurrentSaves_RetainCommitOrderAndLastSaveWins()
    {
        var ct = TestContext.Current.CancellationToken;
        using var store = new ConfigurationCommandRecorder();
        store.Initialize(new BlogConfig());
        await using var secondDb = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(store.Db.Database.GetDbConnection()).Options);
        var committed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                committed.SetResult();
                await resume.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            });
        var first = store.Accounts.Read();
        first.Username = "first";
        var second = store.Accounts.Read();
        second.Username = "second";
        var firstSave = Handler(store, events.Object).HandleAsync(new UpdateLocalAccountCommand(first), ct);
        await committed.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var secondSave = new UpdateLocalAccountCommandHandler(secondDb, store.Accounts, store.Gate, Mock.Of<IEventMediator>(),
            NullLogger<UpdateLocalAccountCommandHandler>.Instance).HandleAsync(new UpdateLocalAccountCommand(second), ct);
        var secondWasBlocked = !secondSave.IsCompleted;
        resume.SetResult();

        await Task.WhenAll(firstSave, secondSave);

        Assert.True(secondWasBlocked);
        Assert.Equal(second.ToJson(), Stored(store));
        Assert.Equal(second.ToJson(), store.Accounts.Read().ToJson());
    }

    private static Task<DateTime?> Timestamp(ConfigurationCommandRecorder store) => store.Db.BlogConfiguration.AsNoTracking()
        .Where(row => row.CfgKey == nameof(LocalAccountSettings)).Select(row => row.LastModifiedTimeUtc)
        .SingleAsync(TestContext.Current.CancellationToken);

    private sealed class CancelAfterSave(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            return ValueTask.FromResult(result);
        }
    }
}
