using Edi.AspNetCore.Utils;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using OtpNet;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Tests;

public class LocalAccountStateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrepareLocalSetupAsync_LeavesCredentialsUncommittedAndDiagnosticsSafe(bool required)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(required);

        var pending = await store.Accounts.PrepareLocalSetupAsync("  Owner@example.test  ", OwnerAccountTestStore.Password, ct);

        Assert.Equal(OwnerAccountTestStore.Email, pending.Email);
        Assert.Equal(required, pending.RequiresTotp);
        Assert.Null(pending.ExpectedRevision);
        Assert.Equal(SecurityHelper.HashPassword(OwnerAccountTestStore.Password, pending.PasswordSalt), pending.PasswordHash);
        Assert.NotEqual(OwnerAccountTestStore.Password, pending.PasswordHash);
        if (required) Assert.True(store.Totp.VerifyCode(pending.TotpSecret!, Code(pending.TotpSecret!)));
        else Assert.Null(pending.TotpSecret);
        Assert.False(await store.Db.OwnerAccount.AnyAsync(ct));
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(pending.Email, OwnerAccountTestStore.Password, ct));
        Assert.DoesNotContain(OwnerAccountTestStore.Password, pending.ToString());
        Assert.DoesNotContain(pending.PasswordHash, pending.ToString());
        Assert.DoesNotContain(pending.PasswordSalt, pending.ToString());
        if (required) Assert.DoesNotContain(pending.TotpSecret!, pending.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteLocalSetupAsync_ValidProof_CommitsCredentialAndTotpStateAtomically(bool profileOnly)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(required: true);
        var profile = profileOnly ? await store.Accounts.CreateProfileAsync("profile@example.test", ct) : null;
        var pending = await store.Accounts.PrepareLocalSetupAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct);
        Assert.Equal(profile?.Revision, pending.ExpectedRevision);
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(pending.Email, OwnerAccountTestStore.Password, ct));

        var initialized = await store.Accounts.CompleteLocalSetupAsync(pending, Code(pending.TotpSecret!), ct);

        Assert.True(initialized.IsLocalInitialized);
        Assert.True(initialized.IsTotpEnabled);
        if (profileOnly) Assert.Equal(profile!.Id, initialized.Id);
        Assert.Equal(initialized, await store.Accounts.AuthenticateLocalAsync(pending.Email, OwnerAccountTestStore.Password, ct));
        var row = await store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal(pending.PasswordHash, row.PasswordHash);
        Assert.Equal(pending.PasswordSalt, row.PasswordSalt);
        Assert.Equal(pending.TotpSecret, row.TotpSecret);
        Assert.Equal(OwnerAccountTestStore.Email, row.Email);
        Assert.Equal("OWNER@EXAMPLE.TEST", row.NormalizedEmail);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.Accounts.CompleteLocalSetupAsync(pending, Code(pending.TotpSecret!), ct));
        Assert.Equal(initialized, await store.Accounts.ReadAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteLocalSetupAsync_InvalidProof_PreservesAbsentOrProfileOnlyState(bool profileOnly)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(required: true);
        var before = profileOnly ? await store.Accounts.CreateProfileAsync("profile@example.test", ct) : null;
        var pending = await store.Accounts.PrepareLocalSetupAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct);

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            store.Accounts.CompleteLocalSetupAsync(pending, "invalid-code", ct));

        Assert.Equal(before, await store.Accounts.ReadAsync(ct));
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(pending.Email, OwnerAccountTestStore.Password, ct));
        Assert.Empty(store.Db.ChangeTracker.Entries<OwnerAccountEntity>());
        Assert.DoesNotContain(pending.PasswordHash, error.ToString());
        Assert.DoesNotContain(pending.PasswordSalt, error.ToString());
        Assert.DoesNotContain(pending.TotpSecret!, error.ToString());
        store.Db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "Unrelated", CfgValue = "{}" });
        await store.Db.SaveChangesAsync(ct);
        Assert.Equal(before, await store.Accounts.ReadAsync(ct));
    }

    [Theory]
    [InlineData(599, false)]
    [InlineData(600, true)]
    [InlineData(601, true)]
    public async Task CompleteLocalSetupAsync_ExpiryBoundary_EnforcesTenMinuteProofLifetime(int elapsedSeconds, bool expired)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var pending = await store.Accounts.PrepareLocalSetupAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct);
        store.Clock.Advance(TimeSpan.FromSeconds(elapsedSeconds));

        if (expired)
        {
            await Assert.ThrowsAsync<ValidationException>(() => store.Accounts.CompleteLocalSetupAsync(pending, null, ct));
            Assert.False(await store.Db.OwnerAccount.AnyAsync(ct));
        }
        else
        {
            var completed = await store.Accounts.CompleteLocalSetupAsync(pending, null, ct);
            Assert.Equal(completed, await store.Accounts.AuthenticateLocalAsync(pending.Email, OwnerAccountTestStore.Password, ct));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteLocalSetupAsync_ChangedTotpRequirement_RejectsPendingAttempt(bool requiredWhenPrepared)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore(requiredWhenPrepared);
        var pending = await store.Accounts.PrepareLocalSetupAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct);
        var changed = store.CreateAccounts(!requiredWhenPrepared);

        await Assert.ThrowsAsync<ValidationException>(() => changed.CompleteLocalSetupAsync(
            pending, requiredWhenPrepared ? Code(pending.TotpSecret!) : null, ct));

        Assert.False(await store.Db.OwnerAccount.AnyAsync(ct));
    }

    [Theory]
    [InlineData("invalid-hash")]
    [InlineData("invalid-salt")]
    [InlineData("empty-hash")]
    [InlineData("empty-salt")]
    [InlineData("oversized-hash")]
    [InlineData("oversized-salt")]
    [InlineData("unexpected-secret")]
    [InlineData("non-utc-expiry")]
    public async Task CompleteLocalSetupAsync_MalformedStagedCredentials_CannotCommitOrLeakIntoLaterSave(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var valid = await store.Accounts.PrepareLocalSetupAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct);
        var pending = new PendingOwnerSetup
        {
            Email = valid.Email,
            ExpectedRevision = valid.ExpectedRevision,
            RequiresTotp = false,
            PasswordHash = scenario switch
            {
                "invalid-hash" => "not-base64",
                "empty-hash" => "",
                "oversized-hash" => new string('A', 260),
                _ => valid.PasswordHash
            },
            PasswordSalt = scenario switch
            {
                "invalid-salt" => "not-base64",
                "empty-salt" => "",
                "oversized-salt" => new string('A', 132),
                _ => valid.PasswordSalt
            },
            TotpSecret = scenario == "unexpected-secret" ? store.Totp.GenerateSecret() : null,
            ExpiresUtc = scenario == "non-utc-expiry"
                ? DateTime.SpecifyKind(valid.ExpiresUtc, DateTimeKind.Unspecified) : valid.ExpiresUtc
        };

        await Assert.ThrowsAsync<ValidationException>(() => store.Accounts.CompleteLocalSetupAsync(pending, null, ct));

        Assert.False(await store.Db.OwnerAccount.AnyAsync(ct));
        Assert.Empty(store.Db.ChangeTracker.Entries<OwnerAccountEntity>());
        store.Db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "Unrelated", CfgValue = "{}" });
        await store.Db.SaveChangesAsync(ct);
        Assert.False(await store.Db.OwnerAccount.AnyAsync(ct));
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(valid.Email, OwnerAccountTestStore.Password, ct));
    }

    [Fact]
    public async Task AbandonedOrCompetingSetup_CannotReplaceCommittedOwner()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var abandoned = await store.Accounts.PrepareLocalSetupAsync("abandoned@example.test", OwnerAccountTestStore.Password, ct);
        Assert.False(await store.Db.OwnerAccount.AnyAsync(ct));
        var winner = await store.InitializeAsync(ct);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => store.Accounts.CompleteLocalSetupAsync(abandoned, null, ct));

        Assert.Equal(winner, await store.Accounts.ReadAsync(ct));
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(abandoned.Email, OwnerAccountTestStore.Password, ct));
    }

    private static string Code(string secret) => new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
}
