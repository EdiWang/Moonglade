using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Web.Pages;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Security.Claims;

namespace Moonglade.Web.Tests.Pages.Auth;

public class SetupAuthenticatorModelTests
{
    [Fact]
    public async Task OnGetAsync_GeneratesProtectedSecretWithoutDatabaseWriteOrExtendingTicket()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync();
        var expires = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds());
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountSetup, properties: new AuthenticationProperties { ExpiresUtc = expires });
        var model = CreateModel(fixture);

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        var ticket = Assert.Single(fixture.SignIns);
        var secret = ticket.Principal.FindFirstValue("moonglade:totp-secret");
        Assert.Equal(secret, model.SetupKey);
        Assert.True(fixture.Totp.VerifyCode(secret!, LocalAccountPageFixture.Code(secret!)));
        Assert.Equal(expires, ticket.Properties.ExpiresUtc);
        Assert.Null((await fixture.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).TotpSecret);
        Assert.Equal(account, await fixture.Accounts.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsBadRequestAndKeepsSetupDisplay()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync();
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountSetup, "JBSWY3DPEHPK3PXP");
        var model = CreateModel(fixture);
        model.ModelState.AddModelError(nameof(SetupAuthenticatorModel.AuthenticatorCode), "Required");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, model.Response.StatusCode);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.Empty(fixture.SignIns);
        Assert.Null((await fixture.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).TotpSecret);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_ReturnsPageWithoutSigningIn()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync();
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountSetup, "JBSWY3DPEHPK3PXP");
        var model = CreateModel(fixture);
        model.AuthenticatorCode = "invalid-code";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.Empty(fixture.SignIns);
        Assert.Equal(account, await fixture.Accounts.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OnPostAsync_ValidCode_EnablesTotpAndSignsInAdmin()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync();
        var secret = fixture.Totp.GenerateSecret();
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountSetup, secret);
        var model = CreateModel(fixture);
        model.AuthenticatorCode = LocalAccountPageFixture.Code(secret);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Dashboard", redirect.PageName);

        var saved = await fixture.Accounts.ReadAsync(TestContext.Current.CancellationToken);
        Assert.True(saved!.IsTotpEnabled);
        Assert.NotEqual(account.Revision, saved.Revision);
        Assert.Equal(secret, (await fixture.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).TotpSecret);
        Assert.Contains(BlogAuthSchemas.LocalAccountSetup, fixture.SignOuts);
        var ticket = Assert.Single(fixture.SignIns);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, ticket.AuthenticationScheme);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, saved));
        Assert.True(ticket.Principal.IsInRole("Administrator"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveFailureOrStaleTicket_DoesNotEnableTotpOrSignIn(bool staleTicket)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync();
        var secret = fixture.Totp.GenerateSecret();
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountSetup, secret);
        var model = CreateModel(fixture);
        model.AuthenticatorCode = LocalAccountPageFixture.Code(secret);
        if (staleTicket)
        {
            await fixture.Accounts.ResetTotpAsync(account.Id, account.Revision, LocalAccountPageFixture.Password, ct);
            var redirect = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());
            Assert.Equal("/SignIn", redirect.PageName);
        }
        else
        {
            await fixture.Db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_account BEFORE UPDATE ON OwnerAccount BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;", ct);
            await Assert.ThrowsAsync<DbUpdateException>(() => model.OnPostAsync());
        }
        Assert.Empty(fixture.SignIns);
        var saved = await fixture.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Null(saved.TotpSecret);
        Assert.True(saved.LocalInitializedUtc.HasValue);
        Assert.Equal(account.Id, saved.Id);
    }

    private static SetupAuthenticatorModel CreateModel(LocalAccountPageFixture fixture) =>
        fixture.Attach(new SetupAuthenticatorModel(fixture.Options, fixture.Accounts, fixture.Totp,
            Mock.Of<ILogger<SetupAuthenticatorModel>>()), BlogAuthSchemas.LocalAccountSetup);

}
