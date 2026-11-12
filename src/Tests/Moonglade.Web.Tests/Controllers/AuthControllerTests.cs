using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using System.Security.Claims;

namespace Moonglade.Web.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task SignOut_SignsOutCookieAndRedirectsToIndex()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        var controller = CreateController(httpContext =>
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
    public void AccessDenied_SetsForbiddenStatusCodeAndReturnsContent()
    {
        var controller = CreateController();

        var result = controller.AccessDenied();

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
        Assert.Equal("Access Denied", contentResult.Content);
    }

    [Fact]
    public void Me_WhenUserIsAuthenticated_ReturnsUserName()
    {
        const string userName = "admin";
        var controller = CreateController(httpContext =>
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
        var controller = CreateController();

        var result = controller.Me();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var userNameValue = okResult.Value!.GetType().GetProperty("UserName")!.GetValue(okResult.Value);
        Assert.Equal("Anonymous", userNameValue);
    }

    private static AuthController CreateController(Action<DefaultHttpContext>? configure = null)
    {
        var controller = new AuthController();

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
