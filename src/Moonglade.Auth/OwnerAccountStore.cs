#nullable enable

using Edi.AspNetCore.Utils;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Moonglade.Auth;

public sealed class OwnerAccountStore(BlogDbContext db) : IOwnerProfileReader
{
    public Task<OwnerProfile?> ReadProfileAsync(CancellationToken ct = default) =>
        db.OwnerAccount.AsNoTracking().Select(account => new OwnerProfile(account.Id, account.Email))
            .SingleOrDefaultAsync(ct);

    public Task<OwnerAccountSnapshot?> ReadAsync(CancellationToken ct = default) =>
        db.OwnerAccount.AsNoTracking().Select(account => new OwnerAccountSnapshot(
            account.Id, account.Email, account.Revision, account.LocalInitializedUtc.HasValue, account.TotpSecret != null))
            .SingleOrDefaultAsync(ct);

    public async Task<OwnerAccountSnapshot> CreateProfileAsync(string email, CancellationToken ct = default)
    {
        var address = NormalizeEmail(email);
        EnsureOwnCommit();
        var account = new OwnerAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = address.Email,
            NormalizedEmail = address.NormalizedEmail,
            Revision = Guid.NewGuid()
        };
        db.OwnerAccount.Add(account);
        await SaveAsync(account, ct);
        return Snapshot(account);
    }

    public async Task<OwnerAccountSnapshot> InitializeLocalAsync(
        string email, string password, Guid? expectedRevision, CancellationToken ct = default)
    {
        var address = NormalizeEmail(email);
        Validator.ValidateValue(password, new ValidationContext(this), [
            new RequiredAttribute(),
            new RegularExpressionAttribute(UpdateLocalAccountPasswordRequest.PasswordPattern)
        ]);
        EnsureOwnCommit();

        var account = await db.OwnerAccount.AsNoTracking().SingleOrDefaultAsync(ct);
        if (account?.LocalInitializedUtc is not null || account?.Revision != expectedRevision)
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
        account.PasswordSalt = SecurityHelper.GenerateSalt();
        account.PasswordHash = SecurityHelper.HashPassword(password, account.PasswordSalt);
        account.LocalInitializedUtc = DateTime.UtcNow;
        account.Revision = Guid.NewGuid();
        await SaveAsync(account, ct);
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
        if (account.PasswordHash is not { } hash || account.PasswordSalt is not { } salt)
            throw new InvalidOperationException("The initialized owner account has incomplete credentials.");

        var actual = Convert.FromBase64String(SecurityHelper.HashPassword(password.Trim(), salt));
        var expected = Convert.FromBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(actual, expected) ? Snapshot(account) : null;
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
