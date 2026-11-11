using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Tests;

public class OwnerAccountStoreTests
{
    private const string Password = "Synthetic123!";

    [Fact]
    public async Task ProfileOnly_CannotAuthenticate_InitializationRetainsIdentityAndNormalizesEmail()
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync(ct);
        var store = new OwnerAccountStore(db);
        Assert.Null(await store.ReadProfileAsync(ct));
        Assert.Null(await store.AuthenticateLocalAsync("owner@example.test", Password, ct));

        var profile = await store.CreateProfileAsync("  Owner@example.test  ", ct);
        Assert.False(profile.IsLocalInitialized);
        Assert.Null(await store.AuthenticateLocalAsync(profile.Email, Password, ct));
        Assert.Equal(new OwnerProfile(profile.Id, "Owner@example.test"), await store.ReadProfileAsync(ct));
        var savedProfile = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal("OWNER@EXAMPLE.TEST", savedProfile.NormalizedEmail);
        Assert.Null(savedProfile.PasswordHash);
        Assert.Null(savedProfile.PasswordSalt);
        Assert.Null(savedProfile.TotpSecret);
        Assert.Null(savedProfile.LocalInitializedUtc);

        var initialized = await store.InitializeLocalAsync("  NewOwner@example.test ", Password, profile.Revision, ct);
        Assert.Equal(profile.Id, initialized.Id);
        Assert.NotEqual(profile.Revision, initialized.Revision);
        Assert.True(initialized.IsLocalInitialized);
        Assert.False(initialized.IsTotpEnabled);
        Assert.Equal(initialized, await store.AuthenticateLocalAsync(" newowner@EXAMPLE.test ", Password, ct));
        Assert.Null(await store.AuthenticateLocalAsync(profile.Email, Password, ct));
        Assert.Null(await store.AuthenticateLocalAsync(initialized.Email, "WrongPassword123!", ct));
        var saved = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.NotEqual(Password, saved.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(saved.PasswordSalt));
        Assert.Equal(DateTimeKind.Utc, saved.LocalInitializedUtc!.Value.Kind);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.InitializeLocalAsync("attacker@example.test", "Replacement123!", initialized.Revision, ct));
        Assert.Equal(initialized, await store.ReadAsync(ct));
    }

    [Fact]
    public async Task EmptyStore_InitializesOnce_AndAuthenticatorResetDoesNotReopenSetup()
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync(ct);
        var store = new OwnerAccountStore(db);

        var initialized = await store.InitializeLocalAsync("owner@example.test", Password, null, ct);
        Assert.NotEqual(Guid.Empty, initialized.Id);
        Assert.NotEqual(Guid.Empty, initialized.Revision);
        Assert.Equal(initialized, await store.AuthenticateLocalAsync(initialized.Email, Password, ct));
        var row = await db.OwnerAccount.SingleAsync(ct);
        row.TotpSecret = "JBSWY3DPEHPK3PXP";
        await db.SaveChangesAsync(ct);
        Assert.True((await store.ReadAsync(ct)).IsTotpEnabled);
        row.TotpSecret = null;
        await db.SaveChangesAsync(ct);
        var reset = await store.ReadAsync(ct);
        Assert.True(reset.IsLocalInitialized);
        Assert.False(reset.IsTotpEnabled);
        Assert.Equal(initialized.Id, reset.Id);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.InitializeLocalAsync("second@example.test", Password, null, ct));
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("invalid", Password)]
    [InlineData("owner@example.test", "short")]
    [InlineData("owner@example.test", "no-digits-here")]
    public async Task InvalidInput_CannotPersistCredentials(string email, string password)
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync(ct);
        var store = new OwnerAccountStore(db);

        await Assert.ThrowsAsync<ValidationException>(() => store.InitializeLocalAsync(email, password, null, ct));
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<OwnerAccountEntity>());
    }

    [Fact]
    public async Task StaleProfileRevision_CannotInitializeOrReplaceProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync(ct);
        var store = new OwnerAccountStore(db);
        var profile = await store.CreateProfileAsync("owner@example.test", ct);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.InitializeLocalAsync("replacement@example.test", Password, null, ct));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.InitializeLocalAsync("replacement@example.test", Password, Guid.NewGuid(), ct));
        await Assert.ThrowsAsync<DbUpdateException>(() => store.CreateProfileAsync("second@example.test", ct));
        Assert.Empty(db.ChangeTracker.Entries<OwnerAccountEntity>());
        Assert.Equal(profile, await store.ReadAsync(ct));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task FailedWrite_PreservesPreviousState_AndCannotLeakIntoLaterSave(bool profileExists, bool cancelled)
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var failure = new WriteFailure(cancelled);
        await using var db = CreateContext(connection, failure);
        await db.Database.EnsureCreatedAsync(ct);
        var store = new OwnerAccountStore(db);
        var before = profileExists ? await store.CreateProfileAsync("original@example.test", ct) : null;
        failure.Enabled = true;

        var exception = await Record.ExceptionAsync(() =>
            store.InitializeLocalAsync("changed@example.test", Password, before?.Revision, ct));
        if (cancelled) Assert.IsType<OperationCanceledException>(exception);
        else Assert.IsType<DbUpdateException>(exception);
        Assert.Equal(before, await store.ReadAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<OwnerAccountEntity>());

        failure.Enabled = false;
        db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "Unrelated", CfgValue = "{}" });
        await db.SaveChangesAsync(ct);
        Assert.Equal(before, await store.ReadAsync(ct));
        Assert.Null(await store.AuthenticateLocalAsync("changed@example.test", Password, ct));
    }

    [Fact]
    public async Task OuterTransaction_IsRejectedBeforeWritingAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync(ct);
        var store = new OwnerAccountStore(db);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateProfileAsync("owner@example.test", ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.InitializeLocalAsync("owner@example.test", Password, null, ct));
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<OwnerAccountEntity>());
    }

    private static BlogDbContext CreateContext(SqliteConnection connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).AddInterceptors(interceptors).Options);

    private sealed class WriteFailure(bool cancelled) : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled)
            {
                if (cancelled) throw new OperationCanceledException();
                throw new DbUpdateException("Injected owner account write failure.");
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
