using Microsoft.EntityFrameworkCore;
using OtpNet;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Tests;

public class CollaboratorAccountStoreTests
{
    private const string TemporaryPassword = "Temporary341!";
    private const string PermanentPassword = "Permanent753!";

    [Fact]
    public async Task CapacityCountsDisabledAccountsAndDeletionReleasesTheSlot()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new OwnerAccountTestStore();
        var owner = await fixture.InitializeAsync(ct);
        var accounts = fixture.CreateLocalAccounts();
        var first = await accounts.CreateCollaboratorAsync("  Editor@Example.test  ", " Editor ", TemporaryPassword, ct);
        var second = await accounts.CreateCollaboratorAsync("second@example.test", "Second", TemporaryPassword, ct);
        Assert.Equal("Editor@Example.test", first.Email);
        Assert.Equal("Editor", first.DisplayName);
        Assert.True(first.MustChangePassword);
        Assert.Equal(first, await accounts.AuthenticateAsync("editor@example.test", TemporaryPassword, ct));

        var disabled = await accounts.SetCollaboratorEnabledAsync(first.Id, first.Revision, enabled: false, ct);
        Assert.NotEqual(first.Revision, disabled.Revision);
        Assert.Equal(disabled, await accounts.SetCollaboratorEnabledAsync(first.Id, disabled.Revision, enabled: false, ct));
        Assert.Null(await accounts.AuthenticateAsync(first.Email, TemporaryPassword, ct));
        await Assert.ThrowsAsync<CollaboratorSlotsFullException>(() =>
            accounts.CreateCollaboratorAsync("third@example.test", "Third", TemporaryPassword, ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => accounts.DeleteCollaboratorAsync(first.Id, first.Revision, ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => accounts.DeleteCollaboratorAsync(owner.Id, owner.Revision, ct));
        await accounts.DeleteCollaboratorAsync(first.Id, disabled.Revision, ct);
        var replacement = await accounts.CreateCollaboratorAsync(first.Email, "Replacement", TemporaryPassword, ct);

        Assert.NotEqual(first.Id, replacement.Id);
        Assert.Equal(new[] { replacement, second }, await accounts.ListCollaboratorsAsync(ct));
        Assert.Equal(owner, await fixture.Accounts.ReadAsync(ct));
    }

    [Fact]
    public async Task EmailUniquenessAndFailedMutationsPreserveTheExistingRevision()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new OwnerAccountTestStore();
        var owner = await fixture.InitializeAsync(ct);
        var accounts = fixture.CreateLocalAccounts();

        await Assert.ThrowsAsync<ValidationException>(() =>
            accounts.CreateCollaboratorAsync(owner.Email.ToUpperInvariant(), "Duplicate", TemporaryPassword, ct));
        var editor = await accounts.CreateCollaboratorAsync("editor@example.test", "Editor", TemporaryPassword, ct);
        await Assert.ThrowsAsync<ValidationException>(() =>
            accounts.ChangeCollaboratorEmailAsync(editor.Id, editor.Revision, owner.Email.ToUpperInvariant(), ct));
        Assert.Equal(editor, Assert.Single(await accounts.ListCollaboratorsAsync(ct)));
        Assert.Empty(fixture.Db.ChangeTracker.Entries());

        var changed = await accounts.ChangeCollaboratorEmailAsync(editor.Id, editor.Revision, "new@example.test", ct);
        Assert.NotEqual(editor.Revision, changed.Revision);
        Assert.Null(await accounts.AuthenticateAsync(editor.Email, TemporaryPassword, ct));
        Assert.Equal(changed, await accounts.AuthenticateAsync(changed.Email, TemporaryPassword, ct));
    }

    [Fact]
    public async Task PasswordResetPreservesAuthenticatorAndRequiresItsProofBeforeTheNewPasswordCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new OwnerAccountTestStore();
        await fixture.InitializeAsync(ct);
        var accounts = fixture.CreateLocalAccounts(required: true);
        var temporary = await accounts.CreateCollaboratorAsync("editor@example.test", "Editor", TemporaryPassword, ct);

        await Assert.ThrowsAsync<ValidationException>(() => accounts.CompleteTemporaryPasswordChangeAsync(
            temporary.Id, temporary.Revision, TemporaryPassword, null, ct));
        var permanent = (await accounts.CompleteTemporaryPasswordChangeAsync(
            temporary.Id, temporary.Revision, PermanentPassword, null, ct))!;
        Assert.False(permanent.MustChangePassword);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => accounts.CompleteTemporaryPasswordChangeAsync(
            temporary.Id, temporary.Revision, "Replacement123!", null, ct));

        var secret = fixture.Totp.GenerateSecret();
        var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
        var enrolled = (await accounts.EnrollTotpAsync(permanent, secret, code, ct))!;
        var reset = await accounts.ResetCollaboratorPasswordAsync(enrolled.Id, enrolled.Revision, TemporaryPassword, ct);
        Assert.True(reset.IsTotpEnabled);
        Assert.True(reset.MustChangePassword);
        Assert.Null(await accounts.CompleteTemporaryPasswordChangeAsync(reset.Id, reset.Revision, PermanentPassword, null, ct));
        Assert.Equal(reset, await accounts.AuthenticateAsync(reset.Email, TemporaryPassword, ct));

        var completed = (await accounts.CompleteTemporaryPasswordChangeAsync(reset.Id, reset.Revision, PermanentPassword, code, ct))!;
        Assert.False(completed.MustChangePassword);
        Assert.True(completed.IsTotpEnabled);
        Assert.Null(await accounts.AuthenticateAsync(reset.Email, TemporaryPassword, ct));
        var resetTotp = await accounts.ResetCollaboratorTotpAsync(completed.Id, completed.Revision, ct);
        Assert.False(resetTotp.IsTotpEnabled);
        Assert.Equal(resetTotp, await accounts.AuthenticateAsync(reset.Email, PermanentPassword, ct));
    }
}
