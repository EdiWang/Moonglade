#nullable enable annotations

using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Tests;

public partial class OwnerAccountOperationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProfileEventFailure_RemainsPostCommitFailure_NotMisreportedAsConcurrency(bool existing)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var oidc = store.CreateAccounts(false, AuthenticationProvider.OpenIdConnect);
        var before = existing ? await oidc.UpdateProfileEmailAsync(null, "old@example.test", ct) : null;
        var failure = new DbUpdateException("Injected event publication failure.");
        store.Events.Setup(events => events.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .ThrowsAsync(failure);

        var actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            oidc.UpdateProfileEmailAsync(before?.Revision, "committed@example.test", ct));

        Assert.Same(failure, actual);
        var saved = await oidc.ReadAsync(ct);
        Assert.Equal("committed@example.test", saved!.Email);
        Assert.False(saved.IsLocalInitialized);
        if (existing)
        {
            Assert.Equal(before!.Id, saved.Id);
            Assert.NotEqual(before.Revision, saved.Revision);
        }
        Assert.Empty(store.Db.ChangeTracker.Entries<OwnerAccountEntity>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeLocalEmail_ValidPassword_PreservesCredentialsIdentityAndAuthenticator(bool withTotp)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(withTotp);
        var before = await InitializeAsync(store, withTotp, ct);
        var credentials = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        store.Events.Invocations.Clear();

        var changed = await store.Accounts.ChangeLocalEmailAsync(before.Id, before.Revision,
            OwnerAccountTestStore.Password, "  NewOwner@example.test ", ct);

        Assert.Equal(before.Id, changed!.Id);
        Assert.Equal("NewOwner@example.test", changed.Email);
        Assert.NotEqual(before.Revision, changed.Revision);
        Assert.Equal(withTotp, changed.IsTotpEnabled);
        Assert.True(changed.IsLocalInitialized);
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(before.Email, OwnerAccountTestStore.Password, ct));
        Assert.Equal(changed, await store.Accounts.AuthenticateLocalAsync(" newowner@EXAMPLE.TEST ", OwnerAccountTestStore.Password, ct));
        var saved = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal("NEWOWNER@EXAMPLE.TEST", saved.NormalizedEmail);
        Assert.Equal((credentials.PasswordHash, credentials.PasswordSalt, credentials.TotpSecret, credentials.LocalInitializedUtc),
            (saved.PasswordHash, saved.PasswordSalt, saved.TotpSecret, saved.LocalInitializedUtc));
        if (withTotp) Assert.Equal(changed, await store.Accounts.VerifyTotpAsync(changed.Id, changed.Revision, Code(saved.TotpSecret!), ct));
        Assert.Equal(new OwnerProfile(changed.Id, changed.Email), await store.Accounts.ReadProfileAsync(ct));
        Assert.IsType<OwnerProfileChangedEvent>(Assert.Single(store.Events.Invocations).Arguments[0]);
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("missing-password")]
    [InlineData("no-op")]
    public async Task ChangeLocalEmail_RejectedPasswordOrNoOp_RetainsExactRevisionAndDoesNotPublish(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var before = await store.InitializeAsync(ct);
        store.Events.Invocations.Clear();
        var password = scenario == "no-op" ? OwnerAccountTestStore.Password : scenario == "missing-password" ? null : "Wrong456!";

        var result = await store.Accounts.ChangeLocalEmailAsync(before.Id, before.Revision,
            password!, $" {before.Email} ", ct);

        if (scenario == "no-op") Assert.Equal(before, result);
        else Assert.Null(result);
        Assert.Equal(before, await store.Accounts.ReadAsync(ct));
        Assert.Equal(before, await store.Accounts.AuthenticateLocalAsync(before.Email, OwnerAccountTestStore.Password, ct));
        Assert.Empty(store.Events.Invocations);
        Assert.Empty(store.Db.ChangeTracker.Entries<OwnerAccountEntity>());
    }

    [Theory]
    [InlineData("stale")]
    [InlineData("wrong-id")]
    [InlineData("invalid-email")]
    public async Task ChangeLocalEmail_StaleIdentityOrInvalidAddress_CannotChangeAccount(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var before = await store.InitializeAsync(ct);
        store.Events.Invocations.Clear();
        var error = await Record.ExceptionAsync(() => store.Accounts.ChangeLocalEmailAsync(
            scenario == "wrong-id" ? Guid.NewGuid() : before.Id,
            scenario == "stale" ? Guid.NewGuid() : before.Revision, OwnerAccountTestStore.Password,
            scenario == "invalid-email" ? "invalid" : "new@example.test", ct));

        if (scenario == "invalid-email") Assert.IsType<ValidationException>(error);
        else Assert.IsType<DbUpdateConcurrencyException>(error);
        Assert.Equal(before, await store.Accounts.ReadAsync(ct));
        Assert.Empty(store.Events.Invocations);
    }

    [Fact]
    public async Task UpdateProfileEmail_CreateChangeAndNoOp_AreCredentialFreeAndRevisionGuarded()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var oidc = store.CreateAccounts(false, AuthenticationProvider.OpenIdConnect);
        var created = await oidc.UpdateProfileEmailAsync(null, " Public@example.test ", ct);
        var noOp = await oidc.UpdateProfileEmailAsync(created.Revision, " Public@example.test ", ct);
        Assert.Equal(created, noOp);
        Assert.Single(store.Events.Invocations);

        var updated = await oidc.UpdateProfileEmailAsync(created.Revision, "changed@example.test", ct);

        Assert.Equal(created.Id, updated.Id);
        Assert.NotEqual(created.Revision, updated.Revision);
        Assert.Equal("changed@example.test", updated.Email);
        Assert.False(updated.IsLocalInitialized);
        Assert.False(updated.IsTotpEnabled);
        var row = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal("CHANGED@EXAMPLE.TEST", row.NormalizedEmail);
        Assert.Null(row.PasswordHash);
        Assert.Null(row.PasswordSalt);
        Assert.Null(row.LocalInitializedUtc);
        Assert.Null(await oidc.AuthenticateLocalAsync(updated.Email, OwnerAccountTestStore.Password, ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => oidc.UpdateProfileEmailAsync(null, "racer@example.test", ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => oidc.UpdateProfileEmailAsync(created.Revision, "stale@example.test", ct));
        Assert.Equal(updated, await oidc.ReadAsync(ct));
        Assert.Equal(2, store.Events.Invocations.Count);
    }

    [Fact]
    public async Task UpdateProfileEmail_ExistingInitializedAccount_PreservesAllCredentials()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(true);
        var account = await InitializeAsync(store, true, ct);
        var before = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        var oidc = store.CreateAccounts(true, AuthenticationProvider.OpenIdConnect);

        var changed = await oidc.UpdateProfileEmailAsync(account.Revision, "public@example.test", ct);

        var after = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal((before.Id, before.PasswordHash, before.PasswordSalt, before.TotpSecret, before.LocalInitializedUtc),
            (after.Id, after.PasswordHash, after.PasswordSalt, after.TotpSecret, after.LocalInitializedUtc));
        Assert.Equal(changed, await store.Accounts.AuthenticateLocalAsync(changed.Email, OwnerAccountTestStore.Password, ct));
    }

    [Fact]
    public async Task EmailOperations_WrongProviderAndOuterTransaction_RejectBeforeMutation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(ct);
        var oidc = store.CreateAccounts(false, AuthenticationProvider.OpenIdConnect);
        store.Events.Invocations.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.Accounts.UpdateProfileEmailAsync(account.Revision, "new@example.test", ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => oidc.ChangeLocalEmailAsync(account.Id, account.Revision,
            OwnerAccountTestStore.Password, "new@example.test", ct));
        await using var transaction = await store.Db.Database.BeginTransactionAsync(ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.Accounts.ChangeLocalEmailAsync(account.Id, account.Revision,
            OwnerAccountTestStore.Password, "new@example.test", ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => oidc.UpdateProfileEmailAsync(account.Revision, "new@example.test", ct));
        Assert.Equal(account, await store.Accounts.ReadAsync(ct));
        Assert.Empty(store.Events.Invocations);
    }

    [Theory]
    [InlineData("local", false)]
    [InlineData("profile-update", false)]
    [InlineData("profile-create", false)]
    [InlineData("local", true)]
    [InlineData("profile-update", true)]
    [InlineData("profile-create", true)]
    public async Task FailedOrCancelledEmailWrite_DoesNotPublishOrReappearInLaterSave(string operation, bool cancelled)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var account = operation == "profile-create" ? null : await store.InitializeAsync(ct);
        var oidc = store.CreateAccounts(false, AuthenticationProvider.OpenIdConnect);
        store.Events.Invocations.Clear();
        using var cancellation = new CancellationTokenSource();
        if (cancelled) await cancellation.CancelAsync();
        else await store.Db.Database.ExecuteSqlRawAsync(account is null
            ? "CREATE TRIGGER reject_email BEFORE INSERT ON OwnerAccount BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;"
            : "CREATE TRIGGER reject_email BEFORE UPDATE ON OwnerAccount BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;", ct);

        var error = await Record.ExceptionAsync(() => operation == "local"
            ? store.Accounts.ChangeLocalEmailAsync(account!.Id, account.Revision, OwnerAccountTestStore.Password,
                "new@example.test", cancelled ? cancellation.Token : ct)
            : oidc.UpdateProfileEmailAsync(account?.Revision, "new@example.test", cancelled ? cancellation.Token : ct));

        if (cancelled) Assert.IsAssignableFrom<OperationCanceledException>(error);
        else Assert.IsType<DbUpdateException>(error);
        Assert.Empty(store.Events.Invocations);
        Assert.Empty(store.Db.ChangeTracker.Entries<OwnerAccountEntity>());
        if (!cancelled) await store.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_email;", ct);
        store.Db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "UnrelatedEmailWrite", CfgValue = "{}" });
        await store.Db.SaveChangesAsync(ct);
        Assert.Equal(account, await store.Accounts.ReadAsync(ct));
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("setup")]
    [InlineData("local-change")]
    public async Task OwnerChangedEvent_SeesDurablePublicProfile_WithNoncancelableHandoff(string operation)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var current = operation == "local-change" ? await store.InitializeAsync(ct) : null;
        store.Events.Invocations.Clear();
        OwnerProfile? observed = null;
        store.Events.Setup(events => events.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Returns(async (IEvent message, EventMediationSettings _, CancellationToken token) =>
            {
                Assert.IsType<OwnerProfileChangedEvent>(message);
                Assert.False(token.CanBeCanceled);
                Assert.Empty(store.Db.ChangeTracker.Entries<OwnerAccountEntity>());
                observed = await store.Accounts.ReadProfileAsync(ct);
            });

        var saved = operation switch
        {
            "profile" => await store.Accounts.CreateProfileAsync("public@example.test", ct),
            "setup" => await store.InitializeAsync(ct),
            _ => await store.Accounts.ChangeLocalEmailAsync(current!.Id, current.Revision,
                OwnerAccountTestStore.Password, "new@example.test", ct)
        };

        Assert.Equal(new OwnerProfile(saved!.Id, saved.Email), observed);
        Assert.Single(store.Events.Invocations);
    }
}
