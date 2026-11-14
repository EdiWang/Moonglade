using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Data;
using Moq;
using OtpNet;
using System.Security.Claims;

namespace Moonglade.Web.Tests.TestSupport;

internal sealed class LocalAccountPageFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _services;

    public const string Email = "owner@example.test";
    public const string Password = "Synthetic123!";
    public BlogDbContext Db { get; }
    public OwnerAccountStore Accounts { get; }
    public LocalAccountTotpService Totp { get; } = new();
    public Mock<IAuthenticationService> Authentication { get; } = new();
    public IOptions<AuthenticationSettings> Options { get; }
    public Dictionary<string, AuthenticationTicket> Tickets { get; } = [];
    public List<string> SignOuts { get; } = [];
    public List<AuthenticationTicket> SignIns { get; } = [];
    public TestClock Clock { get; } = new();

    public LocalAccountPageFixture(bool required = true)
    {
        _connection.Open();
        Db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
        Options = Microsoft.Extensions.Options.Options.Create(new AuthenticationSettings
        {
            Totp = new TotpAuthenticationSettings { Required = required }
        });
        Accounts = new OwnerAccountStore(Db, Totp, Options, Clock, Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());
        Authentication.Setup(value => value.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns((HttpContext _, string scheme) => Task.FromResult(Tickets.TryGetValue(scheme, out var ticket)
                ? AuthenticateResult.Success(ticket) : AuthenticateResult.NoResult()));
        Authentication.Setup(value => value.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback((HttpContext _, string scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            {
                var ticket = new AuthenticationTicket(principal, properties ?? new AuthenticationProperties(), scheme);
                Tickets[scheme] = ticket;
                SignIns.Add(ticket);
            }).Returns(Task.CompletedTask);
        Authentication.Setup(value => value.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Callback((HttpContext _, string scheme, AuthenticationProperties? _) =>
            {
                Tickets.Remove(scheme);
                SignOuts.Add(scheme);
            }).Returns(Task.CompletedTask);
        _services = new ServiceCollection().AddSingleton(Authentication.Object).BuildServiceProvider();
    }

    public T Attach<T>(T model, string? scheme = null) where T : PageModel
    {
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = _services,
                User = scheme is not null && Tickets.TryGetValue(scheme, out var ticket)
                    ? ticket.Principal : new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        model.Request.Headers.UserAgent = "B2-test-client";
        return model;
    }

    public async Task<OwnerAccountSnapshot> InitializeAsync(bool withTotp = false)
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new OwnerAccountStore(Db, Totp,
            Microsoft.Extensions.Options.Options.Create(new AuthenticationSettings
            {
                Totp = new TotpAuthenticationSettings { Required = withTotp }
            }), Clock, Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());
        var pending = await store.PrepareLocalSetupAsync(Email, Password, ct);
        return await store.CompleteLocalSetupAsync(pending, withTotp ? Code(pending.TotpSecret!) : null, ct);
    }

    public void SetTicket(OwnerAccountSnapshot account, string scheme, string? secret = null, AuthenticationProperties? properties = null)
    {
        var principal = LocalAccountPrincipal.Create(account, scheme);
        if (secret is not null) ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("moonglade:totp-secret", secret));
        Tickets[scheme] = new AuthenticationTicket(principal, properties ?? new AuthenticationProperties(), scheme);
    }

    public static string Code(string secret) => new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
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
