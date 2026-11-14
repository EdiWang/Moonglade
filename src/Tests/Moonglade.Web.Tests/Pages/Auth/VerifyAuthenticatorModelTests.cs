using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Web.Pages;
using Moonglade.Web.Tests.TestSupport;
using Moq;

namespace Moonglade.Web.Tests.Pages.Auth;

public class VerifyAuthenticatorModelTests
{
    [Fact]
    public async Task OnGetAsync_WhenTotpIsNotConfigured_RedirectsToSignIn()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync();
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountTwoFactor);
        var model = CreateModel(fixture);

        var result = await model.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/SignIn", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsBadRequestWithoutSignIn()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync(withTotp: true);
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountTwoFactor);
        var model = CreateModel(fixture);
        model.ModelState.AddModelError(nameof(VerifyAuthenticatorModel.AuthenticatorCode), "Required");
        model.RememberDevice = true;

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, model.Response.StatusCode);
        Assert.Empty(fixture.SignIns);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_ReturnsPageWithoutSignIn()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync(withTotp: true);
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountTwoFactor);
        var model = CreateModel(fixture);
        model.AuthenticatorCode = "invalid-code";
        model.RememberDevice = true;

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Empty(fixture.SignIns);
    }

    [Fact]
    public async Task OnPostAsync_ValidCode_SignsOutTwoFactorAndSignsInAdmin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync(withTotp: true);
        fixture.SetTicket(account, BlogAuthSchemas.LocalAccountTwoFactor);
        var model = CreateModel(fixture);
        var row = await fixture.Db.OwnerAccount.FindAsync([account.Id], ct);
        model.AuthenticatorCode = LocalAccountPageFixture.Code(row!.TotpSecret!);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Dashboard", redirect.PageName);
        Assert.Contains(BlogAuthSchemas.LocalAccountTwoFactor, fixture.SignOuts);
        var ticket = Assert.Single(fixture.SignIns);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, ticket.AuthenticationScheme);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
        Assert.True(ticket.Principal.IsInRole("Administrator"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnPostAsync_StaleOrMissingTicket_DoesNotSignIn(bool stale)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync(withTotp: true);
        if (stale) fixture.SetTicket(account, BlogAuthSchemas.LocalAccountTwoFactor);
        await fixture.Accounts.ResetTotpAsync(account.Id, account.Revision, LocalAccountPageFixture.Password, ct);
        var model = CreateModel(fixture);
        model.AuthenticatorCode = "invalid-code";

        var redirect = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync());

        Assert.Equal("/SignIn", redirect.PageName);
        Assert.Empty(fixture.SignIns);
    }

    private static VerifyAuthenticatorModel CreateModel(LocalAccountPageFixture fixture) =>
        fixture.Attach(new VerifyAuthenticatorModel(fixture.Options, fixture.Accounts,
            Mock.Of<ILogger<VerifyAuthenticatorModel>>()), BlogAuthSchemas.LocalAccountTwoFactor);
}
