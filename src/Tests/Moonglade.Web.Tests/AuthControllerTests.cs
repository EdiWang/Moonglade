using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task SignOut_WhenProviderIsOpenIdConnect_ReturnsSignOutResultWithRedirectUri()
    {
        const string callbackUrl = "https://example.test/";
        var controller = CreateController(AuthenticationProvider.OpenIdConnect, configure: httpContext =>
        {
            httpContext.Request.Scheme = "https";
        });

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .SetupGet(x => x.ActionContext)
            .Returns(controller.ControllerContext);
        urlHelper
            .Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Returns(callbackUrl);
        controller.Url = urlHelper.Object;

        var result = await controller.SignOut();

        var signOutResult = Assert.IsType<SignOutResult>(result);
        Assert.Equal(callbackUrl, signOutResult.Properties?.RedirectUri);
        Assert.Equal(
            [CookieAuthenticationDefaults.AuthenticationScheme, BlogAuthSchemas.OpenIdConnect],
            signOutResult.AuthenticationSchemes);
    }

    [Fact]
    public async Task SignOut_WhenProviderIsLocal_SignsOutCookieAndRedirectsToIndex()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        var controller = CreateController(AuthenticationProvider.Local, configure: httpContext =>
        {
            var services = new ServiceCollection();
            services.AddSingleton(authenticationService.Object);
            httpContext.RequestServices = services.BuildServiceProvider();
        });

        var result = await controller.SignOut();

        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirectResult.PageName);
        authenticationService.Verify(
            x => x.SignOutAsync(controller.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, null),
            Times.Once);
        authenticationService.Verify(
            x => x.SignOutAsync(controller.HttpContext, BlogAuthSchemas.LocalAccountSetup, null),
            Times.Once);
        authenticationService.Verify(
            x => x.SignOutAsync(controller.HttpContext, BlogAuthSchemas.LocalAccountTwoFactor, null),
            Times.Once);
    }

    [Fact]
    public async Task SignOut_WhenProviderIsUnknown_RedirectsToIndex()
    {
        var controller = CreateController((AuthenticationProvider)999);

        var result = await controller.SignOut();

        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirectResult.PageName);
    }

    [Fact]
    public void AccessDenied_SetsForbiddenStatusCodeAndReturnsContent()
    {
        var controller = CreateController(AuthenticationProvider.Local);

        var result = controller.AccessDenied();

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
        Assert.Equal("Access Denied", contentResult.Content);
    }

    [Fact]
    public void Me_WhenUserIsAuthenticated_ReturnsUserName()
    {
        const string userName = "admin";
        var controller = CreateController(AuthenticationProvider.Local, configure: httpContext =>
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], "TestAuth"));
        });

        var result = controller.Me();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var userNameValue = okResult.Value!.GetType().GetProperty("UserName")!.GetValue(okResult.Value);
        Assert.Equal(userName, userNameValue);
    }

    [Fact]
    public void Me_WhenUserIsAnonymous_ReturnsAnonymous()
    {
        var controller = CreateController(AuthenticationProvider.Local);

        var result = controller.Me();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var userNameValue = okResult.Value!.GetType().GetProperty("UserName")!.GetValue(okResult.Value);
        Assert.Equal("Anonymous", userNameValue);
    }

    [Fact]
    public void Identity_WhenOpenIdConnectUserIsAuthenticated_ReturnsStableIdentityClaims()
    {
        var controller = CreateController(AuthenticationProvider.OpenIdConnect, configure: httpContext =>
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("iss", "https://identity.example.com/"),
                new Claim("sub", "allowed-subject"),
                new Claim(ClaimTypes.Name, "Admin User")
            ],
            BlogAuthSchemas.OpenIdConnect));
        });

        var result = controller.Identity();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(
            "https://identity.example.com/",
            okResult.Value!.GetType().GetProperty("Issuer")!.GetValue(okResult.Value));
        Assert.Equal(
            "allowed-subject",
            okResult.Value.GetType().GetProperty("Subject")!.GetValue(okResult.Value));
        Assert.Equal(
            "Admin User",
            okResult.Value.GetType().GetProperty("DisplayName")!.GetValue(okResult.Value));
    }

    [Fact]
    public void Identity_WhenLocalAuthenticationIsConfigured_ReturnsNotFound()
    {
        var controller = CreateController(AuthenticationProvider.Local);

        var result = controller.Identity();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Identity_WhenRequiredClaimsAreMissing_ReturnsProblemDetails()
    {
        var controller = CreateController(AuthenticationProvider.OpenIdConnect, configure: httpContext =>
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "Admin User")],
                BlogAuthSchemas.OpenIdConnect));
        });

        var result = controller.Identity();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    private static AuthController CreateController(AuthenticationProvider provider, Action<DefaultHttpContext>? configure = null)
    {
        var controller = new AuthController(Options.Create(new AuthenticationSettings
        {
            Provider = provider
        }));

        var httpContext = new DefaultHttpContext();
        configure?.Invoke(httpContext);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };

        return controller;
    }
}
