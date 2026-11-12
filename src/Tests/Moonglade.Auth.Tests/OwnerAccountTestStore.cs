using LiteBus.Events.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using Moq;

namespace Moonglade.Auth.Tests;

internal sealed class OwnerAccountTestStore : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public const string Email = "Owner@example.test";
    public const string Password = "Synthetic123!";
    public BlogDbContext Db { get; }
    public TestClock Clock { get; } = new();
    public LocalAccountTotpService Totp { get; } = new();
    public OwnerAccountStore Accounts { get; }
    public Mock<IEventMediator> Events { get; } = new();

    public OwnerAccountTestStore(bool required = false, params IInterceptor[] interceptors)
    {
        _connection.Open();
        Db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(_connection).AddInterceptors(interceptors).Options);
        Db.Database.EnsureCreated();
        Accounts = CreateAccounts(required);
    }

    public OwnerAccountStore CreateAccounts(bool required) =>
        new(Db, Totp, Options.Create(new AuthenticationSettings
        {
            Totp = new TotpAuthenticationSettings { Required = required }
        }), Clock, Events.Object);

    public async Task<OwnerAccountSnapshot> InitializeAsync(CancellationToken ct)
    {
        var pending = await Accounts.PrepareLocalSetupAsync(Email, Password, ct);
        return await Accounts.CompleteLocalSetupAsync(pending, null, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    internal sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }
}
