#nullable enable

using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;
using static Moonglade.Auth.LocalAccountCredentials;

namespace Moonglade.Auth;

public sealed class OwnerAccountStore(
    BlogDbContext db,
    ILocalAccountTotpService totpService,
    IOptions<AuthenticationSettings> options,
    TimeProvider timeProvider,
    IEventMediator events) : IOwnerProfileReader
{
    private IQueryable<BlogAccountEntity> OwnerQuery() =>
        db.BlogAccount.AsNoTracking().Where(account => account.Slot == BlogAccountEntity.OwnerSlot);

    public Task<OwnerPresentation> ReadPresentationAsync(CancellationToken ct = default) => OwnerPresentation.ReadAsync(db, ct);

    public Task<OwnerProfile?> ReadProfileAsync(CancellationToken ct = default) =>
        OwnerQuery().Select(account => new OwnerProfile(account.Id, account.Email)
        {
            UseGravatar = db.OwnerAvatar.Any(avatar => avatar.UseGravatar)
        })
            .SingleOrDefaultAsync(ct);

    public Task<OwnerAccountSnapshot?> ReadAsync(CancellationToken ct = default) =>
        OwnerQuery().Select(account => new OwnerAccountSnapshot(
            account.Id, account.Email, account.Revision, account.LocalInitializedUtc.HasValue, account.TotpSecret != null))
            .SingleOrDefaultAsync(ct);

    public async Task<OwnerAccountSnapshot> CreateProfileAsync(string email, CancellationToken ct = default)
    {
        var address = NormalizeEmail(email, "An owner email is required.");
        EnsureOwnCommit();
        var account = await SaveNewProfileAsync(address, ct);
        await events.PublishAsync(new OwnerProfileChangedEvent(), CancellationToken.None);
        return Snapshot(account);
    }

    private async Task<BlogAccountEntity> SaveNewProfileAsync(
        (string Email, string NormalizedEmail) address, CancellationToken ct)
    {
        var account = new BlogAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = address.Email,
            NormalizedEmail = address.NormalizedEmail,
            Revision = Guid.NewGuid()
        };
        db.BlogAccount.Add(account);
        await LocalAccountPersistence.SaveAndDetachAsync(db, account, ct, CheckCollaboratorEmailConflictAsync);
        return account;
    }

    public async Task<PendingOwnerSetup> PrepareLocalSetupAsync(
        string email, string password, CancellationToken ct = default)
    {
        var address = NormalizeEmail(email, "An owner email is required.");
        ValidatePassword(password);
        var current = await ReadAsync(ct);
        if (current?.IsLocalInitialized == true)
            throw new DbUpdateConcurrencyException("The owner account is already initialized.");
        var credentials = HashPassword(password);
        return new PendingOwnerSetup
        {
            Email = address.Email,
            PasswordHash = credentials.Hash,
            PasswordSalt = credentials.Salt,
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
        var address = NormalizeEmail(pending.Email, "An owner email is required.");
        EnsureOwnCommit();

        var account = await OwnerQuery().SingleOrDefaultAsync(ct);
        if (account?.LocalInitializedUtc is not null || account?.Revision != pending.ExpectedRevision)
            throw new DbUpdateConcurrencyException("The owner account changed. Start account setup again.");

        if (account is null)
        {
            account = new BlogAccountEntity { Id = Guid.NewGuid() };
            db.BlogAccount.Add(account);
        }
        else
        {
            db.BlogAccount.Attach(account);
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
            await LocalAccountPersistence.SaveAndDetachAsync(db, account, ct, CheckCollaboratorEmailConflictAsync);
        }
        catch (DbUpdateException) when (pending.ExpectedRevision is null)
        {
            if (await OwnerQuery().AnyAsync(row => row.LocalInitializedUtc != null, CancellationToken.None))
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
        var account = await OwnerQuery()
            .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, ct);
        if (account?.LocalInitializedUtc is null) return null;
        return PasswordMatches(account, password) ? Snapshot(account) : null;
    }

    public async Task<OwnerAccountSnapshot?> ChangeLocalEmailAsync(
        Guid id, Guid expectedRevision, string currentPassword, string email, CancellationToken ct = default)
    {
        EnsureOwnCommit();
        var address = NormalizeEmail(email, "An owner email is required.");
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        if (string.IsNullOrWhiteSpace(currentPassword) || !PasswordMatches(account, currentPassword)) return null;
        return await SaveEmailAsync(account, address, ct);
    }

    public async Task<bool> VerifyManagementProofAsync(
        System.Security.Claims.ClaimsPrincipal principal, string? password, string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        var account = await OwnerQuery().SingleOrDefaultAsync(ct);
        return account is not null && LocalAccountPrincipal.Matches(principal, Snapshot(account))
            && PasswordMatches(account, password)
            && (account.TotpSecret is not null
                ? totpService.VerifyCode(account.TotpSecret, code)
                : !options.Value.Totp.Required);
    }

    private async Task<OwnerAccountSnapshot> SaveEmailAsync(
        BlogAccountEntity account, (string Email, string NormalizedEmail) address, CancellationToken ct)
    {
        if (account.Email == address.Email) return Snapshot(account);
        db.BlogAccount.Attach(account);
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
        db.BlogAccount.Attach(account);
        (account.PasswordSalt, account.PasswordHash) = HashPassword(newPassword);
        return await SaveUpdatedAsync(account, ct);
    }

    public async Task<OwnerAccountSnapshot?> ResetTotpAsync(
        Guid id, Guid expectedRevision, string currentPassword, CancellationToken ct = default)
    {
        EnsureOwnCommit();
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        if (!PasswordMatches(account, currentPassword)) return null;
        db.BlogAccount.Attach(account);
        account.TotpSecret = null;
        return await SaveUpdatedAsync(account, ct);
    }

    public async Task<OwnerAccountSnapshot?> EnrollTotpAsync(
        Guid id, Guid expectedRevision, string secret, string code, CancellationToken ct = default)
    {
        EnsureOwnCommit();
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        if (!VerifyTotpEnrollment(totpService, account, secret, code)) return null;
        db.BlogAccount.Attach(account);
        account.TotpSecret = NormalizeTotpSecret(secret);
        return await SaveUpdatedAsync(account, ct);
    }

    public async Task<OwnerAccountSnapshot?> VerifyTotpAsync(
        Guid id, Guid expectedRevision, string code, CancellationToken ct = default)
    {
        var account = await LoadCurrentAsync(id, expectedRevision, ct);
        return totpService.VerifyCode(account.TotpSecret, code) ? Snapshot(account) : null;
    }

    private async Task<BlogAccountEntity> LoadCurrentAsync(Guid id, Guid revision, CancellationToken ct)
    {
        var account = await OwnerQuery().SingleOrDefaultAsync(ct);
        if (account?.LocalInitializedUtc is null || account.Id != id || account.Revision != revision)
            throw new DbUpdateConcurrencyException("The owner account changed. Sign in again.");
        return account;
    }

    private async Task<OwnerAccountSnapshot> SaveUpdatedAsync(BlogAccountEntity account, CancellationToken ct)
    {
        EnsureOwnCommit();
        await LocalAccountPersistence.SaveWithNewRevisionAsync(db, account, ct, CheckCollaboratorEmailConflictAsync);
        return Snapshot(account);
    }

    private async Task CheckCollaboratorEmailConflictAsync(BlogAccountEntity account, DbUpdateException exception)
    {
        if (await db.BlogAccount.AsNoTracking().AnyAsync(row => row.Slot != BlogAccountEntity.OwnerSlot
                && row.NormalizedEmail == account.NormalizedEmail, CancellationToken.None))
            throw new ValidationException("This email address is already in use.", exception);
    }

    private void EnsureOwnCommit()
    {
        if (db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("Owner account writes must own their commit.");
    }

    private static OwnerAccountSnapshot Snapshot(BlogAccountEntity account) =>
        new(account.Id, account.Email, account.Revision, account.LocalInitializedUtc.HasValue, account.TotpSecret is not null);
}
