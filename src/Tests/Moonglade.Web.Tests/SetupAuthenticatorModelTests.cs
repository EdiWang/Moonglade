using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Web.Pages;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests;

public class SetupAuthenticatorModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task OnGetAsync_GeneratesSecretAndBuildsAuthenticatorDisplay()
    {
        using var commandMediator = new ConfigurationCommandRecorder();
        var model = CreateModel(
            LocalAccountSettings.DefaultValue,
            commandMediator,
            new FakeTotpService { GeneratedSecret = "JBSWY3DPEHPK3PXP" });

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.StartsWith("data:image/png;base64,", model.QrCodeImageUri);

        var settings = JsonSerializer.Deserialize<LocalAccountSettings>(
            commandMediator.Single<UpdateConfigurationCommand>().Settings[nameof(LocalAccountSettings)],
            JsonOptions)!;
        Assert.Equal("JBSWY3DPEHPK3PXP", settings.TotpSecret);
        Assert.False(settings.IsTotpEnabled);
    }

    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsBadRequestAndKeepsSetupDisplay()
    {
        using var commandMediator = new ConfigurationCommandRecorder();
        var authenticationService = new Mock<IAuthenticationService>();
        var model = CreateModel(
            LocalAccountSettings.DefaultValue,
            commandMediator,
            new FakeTotpService { GeneratedSecret = "JBSWY3DPEHPK3PXP" },
            authenticationService);
        model.ModelState.AddModelError(nameof(SetupAuthenticatorModel.AuthenticatorCode), "Required");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, model.Response.StatusCode);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.StartsWith("data:image/png;base64,", model.QrCodeImageUri);
        authenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_ReturnsPageWithoutSigningIn()
    {
        using var commandMediator = new ConfigurationCommandRecorder();
        var authenticationService = new Mock<IAuthenticationService>();
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = "JBSWY3DPEHPK3PXP";
        var model = CreateModel(
            account,
            commandMediator,
            new FakeTotpService { VerifyResult = false },
            authenticationService);
        model.AuthenticatorCode = "123456";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.Empty(commandMediator.Commands);
        authenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ValidCode_EnablesTotpAndSignsInAdmin()
    {
        using var commandMediator = new ConfigurationCommandRecorder();
        var authenticationService = new Mock<IAuthenticationService>();
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = "JBSWY3DPEHPK3PXP";
        var model = CreateModel(
            account,
            commandMediator,
            new FakeTotpService { VerifyResult = true },
            authenticationService);
        model.AuthenticatorCode = "123456";

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Dashboard", redirect.PageName);

        var settings = JsonSerializer.Deserialize<LocalAccountSettings>(
            commandMediator.Single<UpdateConfigurationCommand>().Settings[nameof(LocalAccountSettings)],
            JsonOptions)!;
        Assert.True(settings.IsTotpEnabled);
        Assert.Equal("JBSWY3DPEHPK3PXP", settings.TotpSecret);

        authenticationService.Verify(
            x => x.SignOutAsync(model.HttpContext, BlogAuthSchemas.LocalAccountSetup, null),
            Times.Once);
        authenticationService.Verify(
            x => x.SignInAsync(
                model.HttpContext,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(p => p.Identity!.Name == account.Username),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveFailure_DoesNotChangeSecretOrEnableTotpOrSignIn(bool hasSecret)
    {
        using var mediator = new ConfigurationCommandRecorder();
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = hasSecret ? "JBSWY3DPEHPK3PXP" : string.Empty;
        var originalJson = account.ToJson();
        var authentication = new Mock<IAuthenticationService>();
        var model = CreateModel(account, mediator, new FakeTotpService { VerifyResult = true }, authentication);
        model.AuthenticatorCode = "123456";
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_account_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => hasSecret ? model.OnPostAsync() : model.OnGetAsync());

        Assert.Same(account, mediator.Config.LocalAccountSettings);
        Assert.Equal(originalJson, account.ToJson());
        authentication.Verify(p => p.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
            It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        authentication.Verify(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
            It.IsAny<AuthenticationProperties>()), Times.Never);
        mediator.Db.ChangeTracker.Clear();
        Assert.Equal(originalJson, (await mediator.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(LocalAccountSettings), TestContext.Current.CancellationToken)).CfgValue);
    }

    private static SetupAuthenticatorModel CreateModel(
        LocalAccountSettings account,
        ConfigurationCommandRecorder commandMediator,
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

        var blogConfig = new BlogConfig { LocalAccountSettings = account };
        commandMediator.Initialize(blogConfig);
        var model = new SetupAuthenticatorModel(
            Options.Create(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.Local,
                Totp = new TotpAuthenticationSettings { Issuer = "Moonglade Test" }
            }),
            blogConfig,
            commandMediator,
            totpService,
            Mock.Of<ILogger<SetupAuthenticatorModel>>());

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }

    private sealed class FakeTotpService : ILocalAccountTotpService
    {
        public string GeneratedSecret { get; init; } = "JBSWY3DPEHPK3PXP";
        public bool VerifyResult { get; init; }

        public string GenerateSecret() => GeneratedSecret;

        public bool VerifyCode(string secret, string code) => VerifyResult;

        public string BuildAuthenticatorUri(string issuer, string accountName, string secret) =>
            $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }

}
