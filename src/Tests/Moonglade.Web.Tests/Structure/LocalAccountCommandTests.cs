using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moonglade.Auth;
using Moonglade.Data;
using Moonglade.Web.Tests.TestSupport;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Tests.Structure;

public class LocalAccountCommandTests
{
    [Fact]
    public async Task PasswordSave_ReturnsCommittedDetachedIdentityWithoutChangingOrdinarySettings()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new LocalAccountPageFixture(required: false);
        var account = await store.InitializeAsync();
        store.Db.BlogConfiguration.Add(new Moonglade.Data.Entities.BlogConfigurationEntity { CfgKey = "GeneralSettings", CfgValue = "retained" });
        await store.Db.SaveChangesAsync(ct);
        var changed = await store.Accounts.ChangePasswordAsync(account.Id, account.Revision,
            LocalAccountPageFixture.Password, "Replacement456!", ct);

        Assert.Equal(changed, await store.Accounts.ReadAsync(ct));
        Assert.Equal(account.Email, changed!.Email);
        Assert.NotEqual(account.Revision, changed.Revision);
        Assert.Equal(account.Id, changed.Id);
        Assert.Equal("retained", (await store.Db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
        Assert.Empty(store.Db.ChangeTracker.Entries<Moonglade.Data.Entities.OwnerAccountEntity>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidCredentialOperation_DoesNotChangeIndependentOrOrdinaryState(bool badPassword)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new LocalAccountPageFixture(required: false);
        var account = await store.InitializeAsync();
        if (badPassword)
            Assert.Null(await store.Accounts.ChangePasswordAsync(account.Id, account.Revision, "WrongPassword456!", "Replacement456!", ct));
        else
            await Assert.ThrowsAsync<ValidationException>(() =>
                store.Accounts.ChangePasswordAsync(account.Id, account.Revision, LocalAccountPageFixture.Password, "short", ct));

        Assert.Equal(account, await store.Accounts.ReadAsync(ct));
        Assert.Equal(account, await store.Accounts.AuthenticateLocalAsync(account.Email, LocalAccountPageFixture.Password, ct));
        Assert.Empty(await store.Db.BlogConfiguration.ToListAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingRowOrFailedSave_CannotProduceACommittedCredentialResult(bool missingRow)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new LocalAccountPageFixture(required: false);
        var account = await store.InitializeAsync();
        if (missingRow)
            await store.Db.OwnerAccount.ExecuteDeleteAsync(ct);
        else
            await store.Db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_account BEFORE UPDATE ON OwnerAccount BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;", ct);

        var save = () => store.Accounts.ChangePasswordAsync(account.Id, account.Revision, LocalAccountPageFixture.Password, "Replacement456!", ct);
        if (missingRow) await Assert.ThrowsAsync<DbUpdateConcurrencyException>(save);
        else await Assert.ThrowsAsync<DbUpdateException>(save);

        Assert.Equal(missingRow ? null : account, await store.Accounts.ReadAsync(ct));
        Assert.Empty(store.Db.ChangeTracker.Entries<Moonglade.Data.Entities.OwnerAccountEntity>());
    }

    [Fact]
    public async Task CancellationBeforeCommit_DetachesCredentialsAndCannotLeakThroughLaterSave()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new LocalAccountPageFixture(required: false);
        var account = await store.InitializeAsync();
        using var cancellation = new CancellationTokenSource();
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(store.Db.Database.GetDbConnection()).AddInterceptors(new CancelBeforeSave(cancellation)).Options);
        var accounts = new OwnerAccountStore(db, store.Totp, store.Options, store.Clock, Moq.Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => accounts.ChangePasswordAsync(account.Id, account.Revision,
            LocalAccountPageFixture.Password, "Replacement456!", cancellation.Token));

        Assert.Empty(db.ChangeTracker.Entries<Moonglade.Data.Entities.OwnerAccountEntity>());
        db.BlogConfiguration.Add(new Moonglade.Data.Entities.BlogConfigurationEntity { CfgKey = "unrelated", CfgValue = "durable" });
        await db.SaveChangesAsync(ct);
        Assert.Equal(account, await store.Accounts.AuthenticateLocalAsync(account.Email, LocalAccountPageFixture.Password, ct));
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(account.Email, "Replacement456!", ct));
        Assert.Equal("durable", (await db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
    }

    [Fact]
    public async Task CancellationAfterCommit_ReturnsDurableIndependentAccountSnapshot()
    {
        await using var store = new LocalAccountPageFixture(required: false);
        var account = await store.InitializeAsync();
        using var cancellation = new CancellationTokenSource();
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(store.Db.Database.GetDbConnection()).AddInterceptors(new CancelAfterSave(cancellation)).Options);
        var accounts = new OwnerAccountStore(db, store.Totp, store.Options, store.Clock, Moq.Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());
        var changed = await accounts.ChangePasswordAsync(account.Id, account.Revision,
            LocalAccountPageFixture.Password, "Replacement456!", cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal(changed, await store.Accounts.AuthenticateLocalAsync(account.Email, "Replacement456!", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StaleCapturedRevision_CannotOverwriteEarlierPasswordCommit()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new LocalAccountPageFixture(required: false);
        var account = await store.InitializeAsync();
        await using var secondDb = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(store.Db.Database.GetDbConnection()).Options);
        var second = new OwnerAccountStore(secondDb, store.Totp, store.Options, store.Clock, Moq.Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());
        var captured = await second.ReadAsync(ct);
        var first = await store.Accounts.ChangePasswordAsync(account.Id, account.Revision, LocalAccountPageFixture.Password, "Replacement456!", ct);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            second.ChangePasswordAsync(captured!.Id, captured.Revision, "Replacement456!", "Attacker789!", ct));
        Assert.Equal(first, await second.AuthenticateLocalAsync(account.Email, "Replacement456!", ct));
        Assert.Null(await second.AuthenticateLocalAsync(account.Email, "Attacker789!", ct));
    }

    private sealed class CancelAfterSave(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CancelBeforeSave(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        private bool _fired;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_fired)
            {
                _fired = true;
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return ValueTask.FromResult(result);
        }
    }
}
