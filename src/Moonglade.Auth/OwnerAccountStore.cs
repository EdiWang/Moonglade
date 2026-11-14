#nullable enable

using Edi.AspNetCore.Utils;
using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Moonglade.Auth;

public sealed class OwnerAccountStore(
    BlogDbContext db,
    ILocalAccountTotpService totpService,
    IOptions<AuthenticationSettings> options,
    TimeProvider timeProvider,
    IEventMediator events) : IOwnerProfileReader
{
    public Task<OwnerPresentation> ReadPresentationAsync(CancellationToken ct = default) => OwnerPresentation.ReadAsync(db, ct);

    public Task<OwnerProfile?> ReadProfileAsync(CancellationToken ct = default) =>
        db.OwnerAccount.AsNoTracking().Select(account => new OwnerProfile(account.Id, account.Email)
        {
            UseGravatar = db.OwnerAvatar.Any(avatar => avatar.UseGravatar)
        })
            .SingleOrDefaultAsync(ct);

    public Task<OwnerAccountSnapshot?> ReadAsync(CancellationToken ct = default) =>
        db.OwnerAccount.AsNoTracking().Select(account => new OwnerAccountSnapshot(
            account.Id, account.Email, account.Revision, account.LocalInitializedUtc.HasValue, account.TotpSecret != null))
            .SingleOrDefaultAsync(ct);

    public async Task<OwnerAccountSnapshot> CreateProfileAsync(string email, CancellationToken ct = default)
    {
        var address = NormalizeEmail(email);
        EnsureOwnCommit();
        var account = await SaveNewProfileAsync(address, ct);
        await events.PublishAsync(new OwnerProfileChangedEvent(), CancellationToken.None);
        return Snapshot(account);
    }

    private async Task<OwnerAccountEntity> SaveNewProfileAsync(
        (string Email, string NormalizedEmail) address, CancellationToken ct)
    {
        var account = new OwnerAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = address.Email,
            NormalizedEmail = address.NormalizedEmail,
            Revision = Guid.NewGuid()
        };
        db.OwnerAccount.Add(account);
        await SaveAsync(account, ct);
        return account;
    }

    public async Task<PendingOwnerSetup> PrepareLocalSetupAsync(
        string email, string password, CancellationToken ct = default)
    {
        var address = NormalizeEmail(email);
        ValidatePassword(password);
        var current = await ReadAsync(ct);
        if (current?.IsLocalInitialized == true)
            throw new DbUpdateConcurrencyException("The owner account is already initialized.");
        var salt = SecurityHelper.GenerateSalt();
        return new PendingOwnerSetup
        {
            Email = address.Email,
            PasswordHash = SecurityHelper.HashPassword(password, salt),
            PasswordSalt = salt,
            ExpectedRevision = current?.Revision,
            RequiresTotp = options.Value.Totp.Required,
            TotpSecret = options.Value.Totp.Required ? totpService.GenerateSecret() : null,
            ExpiresUtc = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(10)
        };
    }

    public async Task<OwnerAccountSnapshot> CompleteLocalSetupAsync(
        PendingOwnerSetup pending, string? code, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pending);
        if (pending.ExpiresUtc.Kind != DateTimeKind.Utc || pending.ExpiresUtc <= timeProvider.GetUtcNow().UtcDateTime
            || pending.RequiresTotp != options.Value.Totp.Required)
            throw new ValidationException("Account setup has expired. Start again.");
        if (pending.RequiresTotp && !totpService.VerifyCode(pending.TotpSecret, code))
            throw new ValidationException("Invalid authenticator code.");
        if (string.IsNullOrWhiteSpace(pending.PasswordHash) || string.IsNullOrWhiteSpace(pending.PasswordSalt)
            || pending.PasswordHash.Length > 256 || pending.PasswordSalt.Length > 128
            || (!pending.RequiresTotp && pending.TotpSecret is not null))
            throw new ValidationException("Account setup is invalid. Start again.");
        try
        {
            if (Convert.FromBase64String(pending.PasswordHash).Length == 0
                || Convert.FromBase64String(pending.PasswordSalt).Length == 0)
                throw new ValidationException("Account setup is invalid. Start again.");
        }
        catch (FormatException)
        {
            throw new ValidationException("Account setup is invalid. Start again.");
        }
        var address = NormalizeEmail(pending.Email);
        EnsureOwnCommit();

        var account = await db.OwnerAccount.AsNoTracking().SingleOrDefaultAsync(ct);
        if (account?.LocalInitializedUtc is not null || account?.Revision != pending.ExpectedRevision)
            throw new DbUpdateConcurrencyException("The owner account changed. Start account setup again.");

        if (account is null)
        {
            account = new OwnerAccountEntity { Id = Guid.NewGuid() };
            db.OwnerAccount.Add(account);
        }
        else
        {
            db.OwnerAccount.Attach(account);
            db.Entry(account).State = EntityState.Modified;
        }

        account.Email = address.Email;
        account.NormalizedEmail = address.NormalizedEmail;
        account.PasswordSalt = pending.PasswordSalt;
        account.PasswordHash = pending.PasswordHash;
        account.TotpSecret = pending.RequiresTotp ? pending.TotpSecret : null;
        account.LocalInitializedUtc = timeProvider.GetUtcNow().UtcDateTime;
        account.Revision = Guid.NewGuid();
        try
        {
            await SaveAsync(account, ct);
        }
        catch (DbUpdateException) when (pending.ExpectedRevision is null)
        {
            if (await db.OwnerAccount.AsNoTracking().AnyAsync(row => row.LocalInitializedUtc != null, CancellationToken.None))
                throw new DbUpdateConcurrencyException("The owner account is already initialized.");
            throw;
        }
        await events.PublishAsync(new OwnerProfileChangedEvent(), CancellationToken.None);
        return Snapshot(account);
    }

    public async Task<OwnerAccountSnapshot?> AuthenticateLocalAsync(
        string email, string password, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var account = await db.OwnerAccount.AsNoTracking()
            .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, ct);
        if (account?.LocalInitializedUtc is null) return null;
        return PasswordMatches(account, password) ? Snapshot(account) : null;
    }

    public async Task<OwnerAccountSnapshot?> ChangeLocalEmailAsync(
        Guid id, Guid expectedRevision, string currentPassword, string email, CancellationToken ct = default)
    {
        EnsureOwnCommit();
        var address = NormalizeEmail(email);
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        if (string.IsNullOrWhiteSpace(currentPassword) || !PasswordMatches(account, currentPassword)) return null;
        return await SaveEmailAsync(account, address, ct);
    }

    public async Task<bool> VerifyManagementProofAsync(
        System.Security.Claims.ClaimsPrincipal principal, string? password, string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        var account = await db.OwnerAccount.AsNoTracking().SingleOrDefaultAsync(ct);
        return account is not null && LocalAccountPrincipal.Matches(principal, Snapshot(account))
            && PasswordMatches(account, password)
            && (account.TotpSecret is not null
                ? totpService.VerifyCode(account.TotpSecret, code)
                : !options.Value.Totp.Required);
    }

    private async Task<OwnerAccountSnapshot> SaveEmailAsync(
        OwnerAccountEntity account, (string Email, string NormalizedEmail) address, CancellationToken ct)
    {
        if (account.Email == address.Email) return Snapshot(account);
        db.OwnerAccount.Attach(account);
        account.Email = address.Email;
        account.NormalizedEmail = address.NormalizedEmail;
        var saved = await SaveUpdatedAsync(account, ct);
        await events.PublishAsync(new OwnerProfileChangedEvent(), CancellationToken.None);
        return saved;
    }

    public async Task<OwnerAccountSnapshot?> ChangePasswordAsync(
        Guid id, Guid expectedRevision, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        EnsureOwnCommit();
        ValidatePassword(newPassword);
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        if (!PasswordMatches(account, currentPassword)) return null;
        db.OwnerAccount.Attach(account);
        account.PasswordSalt = SecurityHelper.GenerateSalt();
        account.PasswordHash = SecurityHelper.HashPassword(newPassword, account.PasswordSalt);
        return await SaveUpdatedAsync(account, ct);
    }

    public async Task<OwnerAccountSnapshot?> ResetTotpAsync(
        Guid id, Guid expectedRevision, string currentPassword, CancellationToken ct = default)
    {
        EnsureOwnCommit();
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        if (!PasswordMatches(account, currentPassword)) return null;
        db.OwnerAccount.Attach(account);
        account.TotpSecret = null;
        return await SaveUpdatedAsync(account, ct);
    }

    public async Task<OwnerAccountSnapshot?> EnrollTotpAsync(
        Guid id, Guid expectedRevision, string secret, string code, CancellationToken ct = default)
    {
        EnsureOwnCommit();
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        if (account.TotpSecret is not null)
            throw new DbUpdateConcurrencyException("The authenticator is already configured.");
        if (!totpService.VerifyCode(secret, code)) return null;
        db.OwnerAccount.Attach(account);
        account.TotpSecret = secret.Trim().Replace(" ", string.Empty).ToUpperInvariant();
        return await SaveUpdatedAsync(account, ct);
    }

    public async Task<OwnerAccountSnapshot?> VerifyTotpAsync(
        Guid id, Guid expectedRevision, string code, CancellationToken ct = default)
    {
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        return totpService.VerifyCode(account.TotpSecret, code) ? Snapshot(account) : null;
    }

    private async Task<OwnerAccountEntity> LoadCurrentAsync(Guid id, Guid revision, CancellationToken ct)
    {
        var account = await db.OwnerAccount.AsNoTracking().SingleOrDefaultAsync(ct);
        if (account?.LocalInitializedUtc is null || account.Id != id || account.Revision != revision)
            throw new DbUpdateConcurrencyException("The owner account changed. Sign in again.");
        return account;
    }

    private async Task<OwnerAccountSnapshot> SaveUpdatedAsync(OwnerAccountEntity account, CancellationToken ct)
    {
        EnsureOwnCommit();
        account.Revision = Guid.NewGuid();
        await SaveAsync(account, ct);
        return Snapshot(account);
    }

    private static bool PasswordMatches(OwnerAccountEntity account, string password)
    {
        if (account.PasswordHash is not { } hash || account.PasswordSalt is not { } salt)
            throw new InvalidOperationException("The initialized owner account has incomplete credentials.");
        var actual = Convert.FromBase64String(SecurityHelper.HashPassword(password.Trim(), salt));
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(hash));
    }

    private void ValidatePassword(string password)
    {
        Validator.ValidateValue(password, new ValidationContext(this) { DisplayName = "Password" }, [
            new RequiredAttribute(), new RegularExpressionAttribute(UpdateLocalAccountPasswordRequest.PasswordPattern)
        ]);
    }

    private async Task SaveAsync(OwnerAccountEntity account, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            // A failed credential write must not be retried by a later unrelated save.
            db.Entry(account).State = EntityState.Detached;
        }
    }

    private void EnsureOwnCommit()
    {
        if (db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("Owner account writes must own their commit.");
    }

    private static (string Email, string NormalizedEmail) NormalizeEmail(string email)
    {
        if (email is null) throw new ValidationException("An owner email is required.");
        var address = email.Trim();
        Validator.ValidateValue(address, new ValidationContext(typeof(OwnerAccountStore)), [
            new RequiredAttribute(), new EmailAddressAttribute(), new StringLengthAttribute(254)
        ]);
        var normalized = address.ToUpperInvariant();
        Validator.ValidateValue(normalized, new ValidationContext(typeof(OwnerAccountStore)),
            [new StringLengthAttribute(254)]);
        return (address, normalized);
    }

    private static OwnerAccountSnapshot Snapshot(OwnerAccountEntity account) =>
        new(account.Id, account.Email, account.Revision, account.LocalInitializedUtc.HasValue, account.TotpSecret is not null);
}
