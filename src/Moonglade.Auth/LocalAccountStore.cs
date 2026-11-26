#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using static Moonglade.Auth.LocalAccountCredentials;

namespace Moonglade.Auth;

public sealed record LocalAccountSnapshot(Guid Id, string Email, Guid Revision, bool IsOwner,
    bool IsTotpEnabled, bool IsDisabled, bool MustChangePassword, string? DisplayName);

public sealed class CollaboratorSlotsFullException() : InvalidOperationException("Both collaborator slots are in use.");

public sealed class LocalAccountStore(
    BlogDbContext db,
    OwnerAccountStore ownerAccounts,
    ILocalAccountTotpService totpService,
    IOptions<AuthenticationSettings> settings,
    TimeProvider timeProvider)
{
    private IQueryable<BlogAccountEntity> Collaborators =>
        db.BlogAccount.AsNoTracking().Where(row => row.Slot != BlogAccountEntity.OwnerSlot);

    public async Task<LocalAccountSnapshot?> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var account = await db.BlogAccount.AsNoTracking().SingleOrDefaultAsync(
            row => row.NormalizedEmail == normalizedEmail && row.LocalInitializedUtc != null && !row.IsDisabled, ct);
        return account is not null && PasswordMatches(account, password) ? Snapshot(account) : null;
    }

    public async Task<LocalAccountSnapshot?> ReadAsync(ClaimsPrincipal? principal, CancellationToken ct)
    {
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ||
            !Guid.TryParse(principal?.FindFirstValue(LocalAccountPrincipal.RevisionClaim), out var revision))
            return null;

        return await db.BlogAccount.AsNoTracking()
            .Where(row => row.Id == id && row.Revision == revision && row.LocalInitializedUtc != null && !row.IsDisabled)
            .Select(row => new LocalAccountSnapshot(row.Id, row.Email, row.Revision, row.Slot == BlogAccountEntity.OwnerSlot,
                row.TotpSecret != null, row.IsDisabled, row.MustChangePassword, row.DisplayName))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<LocalAccountSnapshot?> EnrollTotpAsync(
        LocalAccountSnapshot proof, string secret, string code, CancellationToken ct)
    {
        if (proof.IsOwner)
        {
            var saved = await ownerAccounts.EnrollTotpAsync(proof.Id, proof.Revision, secret, code, ct);
            return saved is null ? null : proof with { Revision = saved.Revision, IsTotpEnabled = true };
        }

        var account = await LoadActiveCollaboratorAsync(proof.Id, proof.Revision, mustChangePassword: false, ct);
        if (account.TotpSecret is not null)
            throw new DbUpdateConcurrencyException("The authenticator is already configured.");
        if (!totpService.VerifyCode(secret, code)) return null;
        return await UpdateAsync(account,
            row => row.TotpSecret = secret.Trim().Replace(" ", string.Empty).ToUpperInvariant(), ct);
    }

    public async Task<LocalAccountSnapshot?> VerifyTotpAsync(LocalAccountSnapshot proof, string code, CancellationToken ct)
    {
        if (proof.IsOwner)
            return await ownerAccounts.VerifyTotpAsync(proof.Id, proof.Revision, code, ct) is null ? null : proof;

        var account = await LoadActiveCollaboratorAsync(proof.Id, proof.Revision, mustChangePassword: false, ct);
        return totpService.VerifyCode(account.TotpSecret, code) ? Snapshot(account) : null;
    }

    public async Task<LocalAccountSnapshot?> CompleteTemporaryPasswordChangeAsync(
        Guid id, Guid revision, string password, string? code, CancellationToken ct)
    {
        ValidatePassword(password);
        var account = await LoadActiveCollaboratorAsync(id, revision, mustChangePassword: true, ct);
        if (settings.Value.Totp.Required && account.TotpSecret is not null && !totpService.VerifyCode(account.TotpSecret, code))
            return null;
        if (PasswordMatches(account, password))
            throw new ValidationException("Choose a password different from the temporary password.");

        return await SetPasswordAsync(account, password, mustChangePassword: false, ct);
    }

    public async Task<LocalAccountSnapshot?> ChangeCollaboratorPasswordAsync(
        Guid id, Guid revision, string currentPassword, string password, CancellationToken ct)
    {
        ValidatePassword(password);
        var account = await LoadActiveCollaboratorAsync(id, revision, mustChangePassword: false, ct);
        return PasswordMatches(account, currentPassword)
            ? await SetPasswordAsync(account, password, mustChangePassword: false, ct)
            : null;
    }

    public async Task<IReadOnlyList<LocalAccountSnapshot>> ListCollaboratorsAsync(CancellationToken ct)
    {
        var accounts = await Collaborators.OrderBy(row => row.Slot)
            .Select(row => new LocalAccountSnapshot(row.Id, row.Email, row.Revision, false,
                row.TotpSecret != null, row.IsDisabled, row.MustChangePassword, row.DisplayName))
            .ToListAsync(ct);
        return accounts;
    }

    public async Task<LocalAccountSnapshot> CreateCollaboratorAsync(
        string email, string displayName, string temporaryPassword, CancellationToken ct)
    {
        EnsureOwnCommit();
        var address = NormalizeEmail(email, "An account email is required.");
        var name = displayName?.Trim();
        Validator.ValidateValue(name, new ValidationContext(this) { DisplayName = "Display name" },
            [new RequiredAttribute(), new StringLengthAttribute(100)]);
        ValidatePassword(temporaryPassword);

        if (!await db.BlogAccount.AnyAsync(row => row.Slot == BlogAccountEntity.OwnerSlot && row.LocalInitializedUtc != null, ct))
            throw new InvalidOperationException("Complete owner setup before creating collaborators.");

        var credentials = HashPassword(temporaryPassword);
        var account = new BlogAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = address.Email,
            NormalizedEmail = address.NormalizedEmail,
            DisplayName = name,
            Revision = Guid.NewGuid(),
            LocalInitializedUtc = timeProvider.GetUtcNow().UtcDateTime,
            PasswordSalt = credentials.Salt,
            PasswordHash = credentials.Hash,
            MustChangePassword = true
        };

        // A unique index arbitrates concurrent claims. Recheck capacity after a competing insert.
        for (var attempt = 0; attempt < BlogAccountEntity.LastCollaboratorSlot - BlogAccountEntity.OwnerSlot; attempt++)
        {
            if (await db.BlogAccount.AnyAsync(row => row.NormalizedEmail == address.NormalizedEmail, ct))
                throw new ValidationException("This email address is already in use.");
            var occupied = await Collaborators.Select(row => row.Slot).ToListAsync(ct);
            var slot = Enumerable.Range(BlogAccountEntity.OwnerSlot + 1, BlogAccountEntity.LastCollaboratorSlot - BlogAccountEntity.OwnerSlot)
                .FirstOrDefault(candidate => !occupied.Contains(candidate));
            if (slot == 0) throw new CollaboratorSlotsFullException();
            account.Slot = slot;
            db.BlogAccount.Add(account);
            try
            {
                await SaveDetachedAsync(account, ct);
                return Snapshot(account);
            }
            catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
            {
                var persisted = await db.BlogAccount.AsNoTracking()
                    .SingleOrDefaultAsync(row => row.NormalizedEmail == address.NormalizedEmail, ct);
                if (persisted?.Id == account.Id) return Snapshot(persisted);
                if (persisted is not null) throw new ValidationException("This email address is already in use.", ex);
                if (!await db.BlogAccount.AnyAsync(row => row.Slot == slot, ct)) throw;
            }
        }

        if (await Collaborators.CountAsync(ct) == BlogAccountEntity.LastCollaboratorSlot - BlogAccountEntity.OwnerSlot)
            throw new CollaboratorSlotsFullException();
        throw new DbUpdateConcurrencyException("The collaborator slots changed. Reload and try again.");
    }

    public async Task<LocalAccountSnapshot> SetCollaboratorEnabledAsync(Guid id, Guid revision, bool enabled, CancellationToken ct)
    {
        var account = await LoadCollaboratorAsync(id, revision, ct);
        return account.IsDisabled == !enabled
            ? Snapshot(account)
            : await UpdateAsync(account, row => row.IsDisabled = !enabled, ct);
    }

    public async Task<LocalAccountSnapshot> ChangeCollaboratorEmailAsync(Guid id, Guid revision, string email, CancellationToken ct)
    {
        var address = NormalizeEmail(email, "An account email is required.");
        var account = await LoadCollaboratorAsync(id, revision, ct);
        if (account.Email == address.Email) return Snapshot(account);

        try
        {
            return await UpdateAsync(account, row =>
            {
                row.Email = address.Email;
                row.NormalizedEmail = address.NormalizedEmail;
            }, ct);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            if (await db.BlogAccount.AnyAsync(row => row.Id != id && row.NormalizedEmail == address.NormalizedEmail, ct))
                throw new ValidationException("This email address is already in use.", ex);
            throw;
        }
    }

    public async Task<LocalAccountSnapshot> ResetCollaboratorPasswordAsync(
        Guid id, Guid revision, string temporaryPassword, CancellationToken ct)
    {
        ValidatePassword(temporaryPassword);
        var account = await LoadCollaboratorAsync(id, revision, ct);
        return await SetPasswordAsync(account, temporaryPassword, mustChangePassword: true, ct);
    }

    public async Task<LocalAccountSnapshot> ResetCollaboratorTotpAsync(Guid id, Guid revision, CancellationToken ct)
    {
        var account = await LoadCollaboratorAsync(id, revision, ct);
        return await UpdateAsync(account, row => row.TotpSecret = null, ct);
    }

    public async Task DeleteCollaboratorAsync(Guid id, Guid revision, CancellationToken ct)
    {
        EnsureOwnCommit();
        var account = await LoadCollaboratorAsync(id, revision, ct);
        db.BlogAccount.Remove(account);
        await SaveDetachedAsync(account, ct);
    }

    private async Task<BlogAccountEntity> LoadCollaboratorAsync(Guid id, Guid revision, CancellationToken ct) =>
        await Collaborators.SingleOrDefaultAsync(row => row.Id == id && row.Revision == revision, ct)
        ?? throw new DbUpdateConcurrencyException("The collaborator account changed. Reload and try again.");

    private async Task<BlogAccountEntity> LoadActiveCollaboratorAsync(Guid id, Guid revision, bool mustChangePassword, CancellationToken ct)
    {
        var account = await LoadCollaboratorAsync(id, revision, ct);
        if (account.IsDisabled || account.MustChangePassword != mustChangePassword)
            throw new DbUpdateConcurrencyException("The collaborator account changed. Sign in again.");
        return account;
    }

    private Task<LocalAccountSnapshot> SetPasswordAsync(BlogAccountEntity account, string password, bool mustChangePassword, CancellationToken ct)
    {
        var credentials = HashPassword(password);
        return UpdateAsync(account, row =>
        {
            row.PasswordSalt = credentials.Salt;
            row.PasswordHash = credentials.Hash;
            row.MustChangePassword = mustChangePassword;
        }, ct);
    }

    private async Task<LocalAccountSnapshot> UpdateAsync(BlogAccountEntity account, Action<BlogAccountEntity> apply, CancellationToken ct)
    {
        EnsureOwnCommit();
        db.BlogAccount.Attach(account);
        apply(account);
        account.Revision = Guid.NewGuid();
        await SaveDetachedAsync(account, ct);
        return Snapshot(account);
    }

    private async Task SaveDetachedAsync(BlogAccountEntity account, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.Entry(account).State = EntityState.Detached;
        }
    }

    private void EnsureOwnCommit()
    {
        if (db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("Collaborator account writes must own their commit.");
    }

    private static LocalAccountSnapshot Snapshot(BlogAccountEntity row) =>
        new(row.Id, row.Email, row.Revision, row.Slot == BlogAccountEntity.OwnerSlot, row.TotpSecret != null,
            row.IsDisabled, row.MustChangePassword, row.DisplayName);
}
