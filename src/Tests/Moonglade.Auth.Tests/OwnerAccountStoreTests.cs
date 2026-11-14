using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
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
        var store = CreateStore(db);
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

        var initialized = await InitializeAsync(store, "  NewOwner@example.test ", Password, ct);
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
            InitializeAsync(store, "attacker@example.test", "Replacement123!", ct));
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
        var store = CreateStore(db);

        var initialized = await InitializeAsync(store, "owner@example.test", Password, ct);
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
            InitializeAsync(store, "second@example.test", Password, ct));
    }

    [Theory]
    [InlineData("empty-email")]
    [InlineData("invalid-email")]
    [InlineData("short-password")]
    [InlineData("missing-digit")]
    public async Task InvalidInput_CannotPersistCredentials(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync(ct);
        var store = CreateStore(db);
        var email = scenario == "empty-email" ? "" : scenario == "invalid-email" ? "invalid" : "owner@example.test";
        var password = scenario == "short-password" ? "short" : scenario == "missing-digit" ? "no-digits-here" : Password;

        await Assert.ThrowsAsync<ValidationException>(() => InitializeAsync(store, email, password, ct));
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
        var store = CreateStore(db);
        var pendingBeforeProfile = await store.PrepareLocalSetupAsync("replacement@example.test", Password, ct);
        var profile = await store.CreateProfileAsync("owner@example.test", ct);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.CompleteLocalSetupAsync(pendingBeforeProfile, null, ct));
        var pendingProfile = await store.PrepareLocalSetupAsync("replacement@example.test", Password, ct);
        var row = await db.OwnerAccount.SingleAsync(ct);
        row.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var changed = await store.ReadAsync(ct);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            store.CompleteLocalSetupAsync(pendingProfile, null, ct));
        await Assert.ThrowsAsync<DbUpdateException>(() => store.CreateProfileAsync("second@example.test", ct));
        Assert.Empty(db.ChangeTracker.Entries<OwnerAccountEntity>());
        Assert.Equal(changed, await store.ReadAsync(ct));
        Assert.Equal(profile.Id, changed!.Id);
        Assert.Equal(profile.Email, changed.Email);
        Assert.False(changed.IsLocalInitialized);
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
        var store = CreateStore(db);
        var before = profileExists ? await store.CreateProfileAsync("original@example.test", ct) : null;
        failure.Enabled = true;

        var exception = await Record.ExceptionAsync(() =>
            InitializeAsync(store, "changed@example.test", Password, ct));
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
        var store = CreateStore(db);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateProfileAsync("owner@example.test", ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InitializeAsync(store, "owner@example.test", Password, ct));
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<OwnerAccountEntity>());
    }

    private static BlogDbContext CreateContext(SqliteConnection connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).AddInterceptors(interceptors).Options);

    private static OwnerAccountStore CreateStore(BlogDbContext db) =>
        new(db, new LocalAccountTotpService(), Options.Create(new AuthenticationSettings
        {
            Totp = new TotpAuthenticationSettings { Required = false }
        }), TimeProvider.System, Moq.Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());

    private static async Task<OwnerAccountSnapshot> InitializeAsync(
        OwnerAccountStore store, string email, string password, CancellationToken ct)
    {
        var pending = await store.PrepareLocalSetupAsync(email, password, ct);
        return await store.CompleteLocalSetupAsync(pending, null, ct);
    }

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
