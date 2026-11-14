using Edi.AspNetCore.Utils;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using OtpNet;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Tests;

public partial class OwnerAccountOperationTests
{
    [Fact]
    public async Task ManagementProof_RequiresEnrolledAuthenticatorEvenWhenLocalSignInTotpIsOptional()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(ct);
        var secret = store.Totp.GenerateSecret();
        var code = Code(secret);
        var enrolled = await store.Accounts.EnrollTotpAsync(account.Id, account.Revision, secret, code, ct);
        var principal = LocalAccountPrincipal.Create(enrolled!, BlogAuthSchemas.Local);

        Assert.False(await store.Accounts.VerifyManagementProofAsync(principal, OwnerAccountTestStore.Password, null, ct));
        Assert.True(await store.Accounts.VerifyManagementProofAsync(principal, OwnerAccountTestStore.Password, code, ct));

        var reset = await store.Accounts.ResetTotpAsync(account.Id, enrolled!.Revision, OwnerAccountTestStore.Password, ct);
        var afterReset = LocalAccountPrincipal.Create(reset!, BlogAuthSchemas.Local);
        Assert.False(await store.CreateAccounts(true).VerifyManagementProofAsync(afterReset, OwnerAccountTestStore.Password, null, ct));
        Assert.True(await store.Accounts.VerifyManagementProofAsync(afterReset, OwnerAccountTestStore.Password, null, ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangePassword_ValidCurrentPassword_PreservesIdentityAndTotp(bool withTotp)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(withTotp);
        var account = await InitializeAsync(store, withTotp, ct);
        var before = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);

        var changed = await store.Accounts.ChangePasswordAsync(account.Id, account.Revision,
            OwnerAccountTestStore.Password, "Replacement456!", ct);

        Assert.Equal(account.Id, changed!.Id);
        Assert.Equal(account.Email, changed.Email);
        Assert.NotEqual(account.Revision, changed.Revision);
        Assert.Equal(withTotp, changed.IsTotpEnabled);
        Assert.True(changed.IsLocalInitialized);
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(account.Email, OwnerAccountTestStore.Password, ct));
        Assert.Equal(changed, await store.Accounts.AuthenticateLocalAsync(account.Email, "Replacement456!", ct));
        var saved = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal(before.LocalInitializedUtc, saved.LocalInitializedUtc);
        Assert.Equal(before.TotpSecret, saved.TotpSecret);
        Assert.NotEqual(before.PasswordSalt, saved.PasswordSalt);
        Assert.NotEqual(before.PasswordHash, saved.PasswordHash);
        Assert.Equal(SecurityHelper.HashPassword("Replacement456!", saved.PasswordSalt!), saved.PasswordHash);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("reset")]
    public async Task CurrentPassword_Invalid_ReturnsNullWithoutChangingAccount(string operation)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(required: true);
        var account = await InitializeAsync(store, true, ct);
        var before = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);

        var result = operation == "password"
            ? await store.Accounts.ChangePasswordAsync(account.Id, account.Revision, "WrongPassword789!", "Replacement456!", ct)
            : await store.Accounts.ResetTotpAsync(account.Id, account.Revision, "WrongPassword789!", ct);

        Assert.Null(result);
        Assert.Equal(account, await store.Accounts.ReadAsync(ct));
        var saved = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal(before.PasswordHash, saved.PasswordHash);
        Assert.Equal(before.PasswordSalt, saved.PasswordSalt);
        Assert.Equal(before.TotpSecret, saved.TotpSecret);
        Assert.Equal(account, await store.Accounts.AuthenticateLocalAsync(account.Email, OwnerAccountTestStore.Password, ct));
    }

    [Fact]
    public async Task ChangePassword_InvalidReplacement_DoesNotChangeCredentials()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(ct);

        await Assert.ThrowsAsync<ValidationException>(() => store.Accounts.ChangePasswordAsync(
            account.Id, account.Revision, OwnerAccountTestStore.Password, "short", ct));

        Assert.Equal(account, await store.Accounts.AuthenticateLocalAsync(account.Email, OwnerAccountTestStore.Password, ct));
    }

    [Fact]
    public async Task ResetTotp_ValidPassword_ClearsOnlyAuthenticatorWithoutReopeningSetup()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(required: true);
        var account = await InitializeAsync(store, true, ct);
        var before = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        var staleCode = Code(before.TotpSecret!);

        var reset = await store.Accounts.ResetTotpAsync(account.Id, account.Revision, OwnerAccountTestStore.Password, ct);

        Assert.Equal(account.Id, reset!.Id);
        Assert.Equal(account.Email, reset.Email);
        Assert.NotEqual(account.Revision, reset.Revision);
        Assert.True(reset.IsLocalInitialized);
        Assert.False(reset.IsTotpEnabled);
        var saved = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Null(saved.TotpSecret);
        Assert.Equal(before.PasswordHash, saved.PasswordHash);
        Assert.Equal(before.PasswordSalt, saved.PasswordSalt);
        Assert.Equal(before.LocalInitializedUtc, saved.LocalInitializedUtc);
        Assert.Equal(reset, await store.Accounts.AuthenticateLocalAsync(account.Email, OwnerAccountTestStore.Password, ct));
        Assert.Null(await store.Accounts.VerifyTotpAsync(reset.Id, reset.Revision, staleCode, ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.Accounts.VerifyTotpAsync(account.Id, account.Revision, staleCode, ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.Accounts.PrepareLocalSetupAsync("attacker@example.test", "Replacement456!", ct));
    }

    [Fact]
    public async Task EnrollTotp_InvalidThenValidProof_PersistsOnlyVerifiedSecretAndRejectsStaleRevision()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(ct);
        var secret = store.Totp.GenerateSecret();

        Assert.Null(await store.Accounts.EnrollTotpAsync(account.Id, account.Revision, secret, "invalid-code", ct));
        Assert.Equal(account, await store.Accounts.ReadAsync(ct));
        Assert.Null((await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct)).TotpSecret);
        var enabled = await store.Accounts.EnrollTotpAsync(account.Id, account.Revision, secret, Code(secret), ct);

        Assert.Equal(account.Id, enabled!.Id);
        Assert.Equal(account.Email, enabled.Email);
        Assert.NotEqual(account.Revision, enabled.Revision);
        Assert.True(enabled.IsLocalInitialized);
        Assert.True(enabled.IsTotpEnabled);
        Assert.Equal(secret, (await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct)).TotpSecret);
        Assert.Equal(enabled, await store.Accounts.VerifyTotpAsync(enabled.Id, enabled.Revision, Code(secret), ct));
        Assert.Null(await store.Accounts.VerifyTotpAsync(enabled.Id, enabled.Revision, "invalid-code", ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.Accounts.EnrollTotpAsync(enabled.Id, enabled.Revision, secret, Code(secret), ct));
        Assert.Equal(enabled, await store.Accounts.ReadAsync(ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.Accounts.EnrollTotpAsync(account.Id, account.Revision, secret, Code(secret), ct));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("reset")]
    [InlineData("enroll")]
    [InlineData("verify")]
    public async Task ChangedAccountRevision_RejectsEveryStaleCredentialOperation(string operation)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(ct);
        var changed = await store.Accounts.ChangePasswordAsync(account.Id, account.Revision,
            OwnerAccountTestStore.Password, "Replacement456!", ct);
        var secret = store.Totp.GenerateSecret();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => operation switch
        {
            "password" => store.Accounts.ChangePasswordAsync(account.Id, account.Revision, "Replacement456!", "Another789!", ct),
            "reset" => store.Accounts.ResetTotpAsync(account.Id, account.Revision, "Replacement456!", ct),
            "enroll" => store.Accounts.EnrollTotpAsync(account.Id, account.Revision, secret, Code(secret), ct),
            _ => store.Accounts.VerifyTotpAsync(account.Id, account.Revision, Code(secret), ct)
        });

        Assert.Equal(changed, await store.Accounts.ReadAsync(ct));
        Assert.Equal(changed, await store.Accounts.AuthenticateLocalAsync(account.Email, "Replacement456!", ct));
    }

    [Theory]
    [InlineData("password", false)]
    [InlineData("reset", false)]
    [InlineData("enroll", false)]
    [InlineData("password", true)]
    [InlineData("reset", true)]
    [InlineData("enroll", true)]
    public async Task FailedOrCancelledCredentialWrite_CannotReappearInLaterSave(string operation, bool cancelled)
    {
        var ct = TestContext.Current.CancellationToken;
        var required = operation != "enroll";
        await using var store = new OwnerAccountTestStore(required);
        var account = await InitializeAsync(store, required, ct);
        var before = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        using var cancellation = new CancellationTokenSource();
        if (cancelled) await cancellation.CancelAsync();
        else await store.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_owner BEFORE UPDATE ON OwnerAccount BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;", ct);
        var secret = store.Totp.GenerateSecret();
        var token = cancelled ? cancellation.Token : ct;

        var error = await Record.ExceptionAsync(() => operation switch
        {
            "password" => store.Accounts.ChangePasswordAsync(account.Id, account.Revision, OwnerAccountTestStore.Password, "Replacement456!", token),
            "reset" => store.Accounts.ResetTotpAsync(account.Id, account.Revision, OwnerAccountTestStore.Password, token),
            _ => store.Accounts.EnrollTotpAsync(account.Id, account.Revision, secret, Code(secret), token)
        });

        if (cancelled) Assert.IsAssignableFrom<OperationCanceledException>(error);
        else Assert.IsType<DbUpdateException>(error);
        Assert.Empty(store.Db.ChangeTracker.Entries<OwnerAccountEntity>());
        if (!cancelled) await store.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_owner;", ct);
        store.Db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "Unrelated", CfgValue = "{}" });
        await store.Db.SaveChangesAsync(ct);
        Assert.Equal(account, await store.Accounts.ReadAsync(ct));
        var saved = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal(before.PasswordHash, saved.PasswordHash);
        Assert.Equal(before.PasswordSalt, saved.PasswordSalt);
        Assert.Equal(before.TotpSecret, saved.TotpSecret);
    }

    private static async Task<OwnerAccountSnapshot> InitializeAsync(OwnerAccountTestStore store, bool required, CancellationToken ct)
    {
        var pending = await store.Accounts.PrepareLocalSetupAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct);
        return await store.Accounts.CompleteLocalSetupAsync(pending, required ? Code(pending.TotpSecret!) : null, ct);
    }

    private static string Code(string secret) => new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
}
