#nullable enable

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Oidc;

public sealed record OwnerOidcSettings(Guid Revision, string DisplayName, string Authority, string ClientId,
    bool Enabled, string? AccountLabel);

internal sealed record OwnerOidcCredentials(Guid OwnerId, Guid Revision, string Authority, string ClientId, string ClientSecret);

public sealed class OwnerOidcStore
{
    private readonly BlogDbContext _db;
    private readonly OidcSecretProtection _secrets;

    internal OwnerOidcStore(BlogDbContext db, OidcSecretProtection secrets)
    {
        _db = db;
        _secrets = secrets;
    }

    public Task<OwnerOidcSettings?> ReadAsync(CancellationToken ct) =>
        _db.OwnerOidc.AsNoTracking().Select(row => new OwnerOidcSettings(
            row.Revision, row.DisplayName, row.Authority, row.ClientId, row.Enabled, row.AccountLabel)).SingleOrDefaultAsync(ct);

    internal async Task<OwnerOidcCredentials?> ReadBindingCredentialsAsync(CancellationToken ct)
    {
        var row = await _db.OwnerOidc.AsNoTracking().SingleOrDefaultAsync(ct);
        return row is { Enabled: true, Subject: null }
            ? new(row.OwnerId, row.Revision, row.Authority, row.ClientId, _secrets.Protector.Unprotect(row.ProtectedSecret))
            : null;
    }

    public async Task<Guid> SaveAsync(Guid ownerId, Guid? expectedRevision, string? displayName,
        string? authority, string? clientId, string? clientSecret, CancellationToken ct)
    {
        displayName = displayName?.Trim();
        authority = authority?.Trim().TrimEnd('/');
        clientId = clientId?.Trim();
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100 ||
            string.IsNullOrWhiteSpace(clientId) || clientId.Length > 500 ||
            authority is not { Length: <= 500 } || !OidcPublicBackchannel.IsHttpsUrl(authority) ||
            clientSecret is { Length: > 4000 })
            throw new ValidationException("Enter a valid name, public HTTPS authority, client ID and client secret.");

        var row = await LoadAsync(ownerId, expectedRevision, ct);
        var providerChanged = row is not null && (row.Authority != authority || row.ClientId != clientId);
        if (providerChanged && row!.Subject is not null)
            throw new ValidationException("Unbind the external account before changing the authority or client ID.");

        if ((row is null || providerChanged) && string.IsNullOrWhiteSpace(clientSecret))
            throw new ValidationException("Enter a client secret for this provider.");

        var protectedSecret = !string.IsNullOrWhiteSpace(clientSecret)
            ? _secrets.Protector.Protect(clientSecret)
            : row!.ProtectedSecret;

        if (row is null)
        {
            row = new OwnerOidcEntity
            {
                OwnerId = ownerId,
                DisplayName = displayName,
                Authority = authority,
                ClientId = clientId,
                ProtectedSecret = protectedSecret,
                Enabled = true
            };
            _db.OwnerOidc.Add(row);
        }
        else
        {
            row.DisplayName = displayName;
            row.Authority = authority;
            row.ClientId = clientId;
            row.ProtectedSecret = protectedSecret;
        }

        return await SaveChangeAsync(row, ct);
    }

    public async Task BindAsync(Guid ownerId, Guid revision, string issuer, string subject, string label, CancellationToken ct)
    {
        var row = await LoadAsync(ownerId, revision, ct);
        if (row is not { Enabled: true, Subject: null }) throw new DbUpdateConcurrencyException();
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255 || issuer.Length > 500 ||
            !OidcPublicBackchannel.IsHttpsUrl(issuer) || string.IsNullOrWhiteSpace(label) || label.Length > 255)
            throw new ValidationException("The provider returned an invalid external account.");

        row.Issuer = issuer;
        row.Subject = subject;
        row.AccountLabel = label;
        row.SessionRevision = Guid.NewGuid();
        await SaveChangeAsync(row, ct);
    }

    public async Task UnbindAsync(Guid ownerId, Guid revision, CancellationToken ct)
    {
        var row = await LoadAsync(ownerId, revision, ct) ?? throw new DbUpdateConcurrencyException();
        row.Issuer = null;
        row.Subject = null;
        row.AccountLabel = null;
        row.SessionRevision = null;
        await SaveChangeAsync(row, ct);
    }

    public async Task SetEnabledAsync(Guid ownerId, Guid revision, bool enabled, CancellationToken ct)
    {
        var row = await LoadAsync(ownerId, revision, ct) ?? throw new DbUpdateConcurrencyException();
        if (row.Enabled == enabled) return;
        row.Enabled = enabled;
        if (row.Subject is not null) row.SessionRevision = Guid.NewGuid();
        await SaveChangeAsync(row, ct);
    }

    public async Task DeleteAsync(Guid ownerId, Guid revision, CancellationToken ct)
    {
        var row = await LoadAsync(ownerId, revision, ct) ?? throw new DbUpdateConcurrencyException();
        _db.OwnerOidc.Remove(row);
        await SaveChangesAsync(row, ct);
    }

    private async Task<OwnerOidcEntity?> LoadAsync(Guid ownerId, Guid? revision, CancellationToken ct)
    {
        var row = await _db.OwnerOidc.AsNoTracking().SingleOrDefaultAsync(ct);
        if (row?.Revision != revision || (row is not null && row.OwnerId != ownerId))
            throw new DbUpdateConcurrencyException();
        if (row is not null) _db.OwnerOidc.Attach(row);
        return row;
    }

    private async Task<Guid> SaveChangeAsync(OwnerOidcEntity row, CancellationToken ct)
    {
        row.Revision = Guid.NewGuid();
        await SaveChangesAsync(row, ct);
        return row.Revision;
    }

    private async Task SaveChangesAsync(OwnerOidcEntity row, CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        finally
        {
            _db.Entry(row).State = EntityState.Detached;
        }
    }
}
