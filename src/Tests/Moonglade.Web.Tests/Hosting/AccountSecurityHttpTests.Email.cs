using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Web.Tests.TestSupport;
using SkiaSharp;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Moonglade.Web.Tests.Hosting;

public partial class AccountSecurityHttpTests
{
    [Fact]
    public async Task Avatar_Get_RevalidatesTheSameUrlAfterUpload()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await AdminTokenAsync(fixture);

        using var bitmap = new SKBitmap(300, 300);
        bitmap.Erase(SKColors.Blue);
        using var firstImage = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var seeded = await fixture.SendAsync(HttpMethod.Post, "/api/account/avatar",
            JsonContent.Create(Convert.ToBase64String(firstImage.ToArray())), token);
        Assert.Equal(HttpStatusCode.OK, seeded.StatusCode);

        using var first = await fixture.Client.GetAsync("/avatar", ct);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.True(first.Headers.CacheControl?.NoCache);
        var tag = Assert.IsType<System.Net.Http.Headers.EntityTagHeaderValue>(first.Headers.ETag);
        using var unchangedRequest = new HttpRequestMessage(HttpMethod.Get, "/avatar");
        unchangedRequest.Headers.IfNoneMatch.Add(tag);
        using var unchanged = await fixture.Client.SendAsync(unchangedRequest, ct);
        Assert.Equal(HttpStatusCode.NotModified, unchanged.StatusCode);

        bitmap.Erase(SKColors.Red);
        using var secondImage = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        var updatedBytes = secondImage.ToArray();
        using var upload = await fixture.SendAsync(HttpMethod.Post, "/api/account/avatar",
            JsonContent.Create(Convert.ToBase64String(updatedBytes)), token);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        using var refresh = new HttpRequestMessage(HttpMethod.Get, "/avatar");
        refresh.Headers.IfNoneMatch.Add(tag);
        using var updated = await fixture.Client.SendAsync(refresh, ct);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.NotEqual(tag, updated.Headers.ETag);
        Assert.Equal(updatedBytes, await updated.Content.ReadAsByteArrayAsync(ct));
    }

    [Fact]
    public async Task AvatarPreference_RequiresAdminAndAntiforgery_AndPreservesAccountAndOrdinarySettings()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        const string route = "/api/account/avatar/gravatar";
        using var anonymous = await fixture.SendAsync(HttpMethod.Put, route, JsonContent.Create(true));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var noToken = await fixture.SendAsync(HttpMethod.Put, route, JsonContent.Create(true));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAvatar.AnyAsync(ct));

        var token = await AdminTokenAsync(fixture);
        var before = fixture.Store.Config.Capture();
        using var accountPage = await fixture.SendAsync(HttpMethod.Get, "/admin/account");
        Assert.Equal(HttpStatusCode.OK, accountPage.StatusCode);
        using var saved = await fixture.SendAsync(HttpMethod.Put, route, JsonContent.Create(true), token);
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        Assert.True((await fixture.Store.Accounts.ReadProfileAsync(ct))!.UseGravatar);
        Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(ct));
        Assert.Same(before, fixture.Store.Config.Capture());
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task ProfileEndpoint_ValidatesAndUpdatesPublicFieldsWithoutChangingLoginOrAvatar()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await AdminTokenAsync(fixture);
        var avatar = new OwnerAvatarStore(fixture.Store.Db);
        await avatar.SetGravatarAsync(true, ct);
        var snapshot = fixture.Store.Config.Capture();
        var profile = OwnerPresentation.Default with { Name = "New name", Description = "New description" };
        const string route = "/api/account/profile";

        using var invalid = await fixture.SendAsync(HttpMethod.Put, route,
            JsonContent.Create(profile with { Name = new string('x', 33) }), token);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(OwnerPresentation.Default, await fixture.Store.Accounts.ReadPresentationAsync(ct));

        using var saved = await fixture.SendAsync(HttpMethod.Put, route, JsonContent.Create(profile), token);

        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        Assert.Equal(profile, await fixture.Store.Accounts.ReadPresentationAsync(ct));
        Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(ct));
        Assert.True(await avatar.UsesGravatarAsync(ct));
        Assert.Same(snapshot, fixture.Store.Config.Capture());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmailEndpoint_LocalChangeSignsOutAllSchemes_AndPreservesAuthenticator(bool bound)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: bound);
        var before = await InitializeAsync(fixture, bound);
        var credentials = await fixture.Store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        string[] schemes = [CookieAuthenticationDefaults.AuthenticationScheme, BlogAuthSchemas.LocalAccountSetup,
            BlogAuthSchemas.LocalAccountTwoFactor, BlogAuthSchemas.InitialAccountSetup];
        foreach (var scheme in schemes) fixture.SetTicket(LocalAccountPrincipal.Create(before, scheme), scheme);
        var token = await AdminTokenAsync(fixture);

        using var changed = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = " NewOwner@example.test ", Revision = before.Revision, CurrentPassword = LocalAccountPageFixture.Password }), token);

        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Empty(await changed.Content.ReadAsStringAsync(ct));
        var current = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.Equal(before.Id, current!.Id);
        Assert.Equal("NewOwner@example.test", current.Email);
        Assert.NotEqual(before.Revision, current.Revision);
        var saved = await fixture.Store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal((credentials.PasswordHash, credentials.PasswordSalt, credentials.TotpSecret, credentials.LocalInitializedUtc),
            (saved.PasswordHash, saved.PasswordSalt, saved.TotpSecret, saved.LocalInitializedUtc));
        Assert.Equal(bound, current.IsTotpEnabled);
        var cleared = changed.Headers.GetValues("Set-Cookie").ToArray();
        foreach (var scheme in schemes)
        {
            var name = fixture.App.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme).Cookie.Name!;
            Assert.Contains(cleared, cookie => cookie.StartsWith(name + "=;", StringComparison.Ordinal));
        }
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Contains("/auth/signin", admin.Headers.Location!.OriginalString);
        fixture.SetTicket(LocalAccountPrincipal.Create(before, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var stale = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, stale.StatusCode);
        Assert.Null(await fixture.Store.Accounts.AuthenticateLocalAsync(before.Email, LocalAccountPageFixture.Password, ct));
        Assert.Equal(current, await fixture.Store.Accounts.AuthenticateLocalAsync(current.Email, LocalAccountPageFixture.Password, ct));
        if (bound) Assert.Equal(current, await fixture.Store.Accounts.VerifyTotpAsync(
            current.Id, current.Revision, LocalAccountPageFixture.Code(saved.TotpSecret!), ct));
    }

    [Fact]
    public async Task EmailEndpoint_WrongPasswordNoOpAndRevisionConflict_RetainSessionAndAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await AdminTokenAsync(fixture);
        foreach (var scenario in new[] { "wrong-password", "missing-password", "stale", "no-op" })
        {
            using var response = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
                JsonContent.Create(new UpdateOwnerEmailRequest
                {
                    Email = scenario == "no-op" ? $" {account.Email} " : "new@example.test",
                    Revision = scenario == "stale" ? Guid.NewGuid() : account.Revision,
                    CurrentPassword = scenario == "missing-password" ? null : scenario == "wrong-password" ? "Wrong456!" : LocalAccountPageFixture.Password
                }), token);
            Assert.Equal(scenario == "no-op" ? HttpStatusCode.NoContent : HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(ct));
            using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
            Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        }
    }

    [Fact]
    public async Task EmailEndpoint_RequiresAuthenticationAntiforgeryAndValidAddressBeforeMutation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        using var anonymous = await fixture.SendAsync(HttpMethod.Get, "/api/account/email");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var anonymousWrite = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = "anonymous@example.test", Revision = account.Revision, CurrentPassword = LocalAccountPageFixture.Password }));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousWrite.StatusCode);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var noToken = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = "new@example.test", Revision = account.Revision, CurrentPassword = LocalAccountPageFixture.Password }));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        var token = await AdminTokenAsync(fixture);
        using var invalid = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = "invalid", Revision = account.Revision, CurrentPassword = LocalAccountPageFixture.Password }), token);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(ct));
        using var response = await fixture.SendAsync(HttpMethod.Get, "/api/account/email");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.Equal(["email", "revision"], json.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(account.Email, json.RootElement.GetProperty("email").GetString());
        Assert.Equal(account.Revision, json.RootElement.GetProperty("revision").GetGuid());
    }

}
