using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Web.Pages;
using Moq;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class VerifyAuthenticatorModelTests
{
    [Fact]
    public void OnGet_WhenTotpIsNotConfigured_RedirectsToSignIn()
    {
        var model = CreateModel(LocalAccountSettings.DefaultValue, new FakeTotpService());

        var result = model.OnGet();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/SignIn", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsBadRequestWithoutSignIn()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        var model = CreateModel(
            CreateTotpEnabledAccount(),
            new FakeTotpService { VerifyResult = true },
            authenticationService);
        model.ModelState.AddModelError(nameof(VerifyAuthenticatorModel.AuthenticatorCode), "Required");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, model.Response.StatusCode);
        authenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_ReturnsPageWithoutSignIn()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        var model = CreateModel(
            CreateTotpEnabledAccount(),
            new FakeTotpService { VerifyResult = false },
            authenticationService);
        model.AuthenticatorCode = "123456";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        authenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ValidCode_SignsOutTwoFactorAndSignsInAdmin()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        var account = CreateTotpEnabledAccount();
        var model = CreateModel(
            account,
            new FakeTotpService { VerifyResult = true },
            authenticationService);
        model.AuthenticatorCode = "123456";

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Dashboard", redirect.PageName);
        authenticationService.Verify(
            x => x.SignOutAsync(model.HttpContext, BlogAuthSchemas.LocalAccountTwoFactor, null),
            Times.Once);
        authenticationService.Verify(
            x => x.SignInAsync(
                model.HttpContext,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(p => p.Identity!.Name == account.Username),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    private static LocalAccountSettings CreateTotpEnabledAccount()
    {
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = "ABCDEF234567";
        account.IsTotpEnabled = true;
        return account;
    }

    private static VerifyAuthenticatorModel CreateModel(
        LocalAccountSettings account,
        ILocalAccountTotpService totpService,
        Mock<IAuthenticationService>? authenticationService = null)
    {
        authenticationService ??= new Mock<IAuthenticationService>();
        var services = new ServiceCollection()
            .AddSingleton(authenticationService.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };

        var model = new VerifyAuthenticatorModel(
            Options.Create(new AuthenticationSettings { Provider = AuthenticationProvider.Local }),
            new BlogConfig { LocalAccountSettings = account },
            totpService,
            Mock.Of<ILogger<VerifyAuthenticatorModel>>());

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }

    private sealed class FakeTotpService : ILocalAccountTotpService
    {
        public bool VerifyResult { get; init; }

        public string GenerateSecret() => "ABCDEF234567";

        public bool VerifyCode(string secret, string code) => VerifyResult;

        public string BuildAuthenticatorUri(string issuer, string accountName, string secret) => string.Empty;
    }
}
