using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web.Middleware
{
    [TestFixture]
    public class RobotsTxtMiddlewareTests
    {
        [Test]
        public async Task TestNonRobotsTxtRequestPath()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RobotsTxtMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, null);

            Assert.Pass();
        }

        [Test]
        public async Task TestRobotsTxtRequestPathWithContent()
        {
            var blogConfigMock = new Mock<IBlogConfig>();
            blogConfigMock.Setup(c => c.AdvancedSettings).Returns(new AdvancedSettings
            {
                RobotsTxtContent = "996"
            });

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RobotsTxtMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/robots.txt";

            await middleware.Invoke(ctx, blogConfigMock.Object);

            Assert.AreEqual("text/plain", ctx.Response.ContentType);

            Assert.Pass();
        }

        [Test]
        public async Task TestRobotsTxtRequestPathNoContent()
        {
            var blogConfigMock = new Mock<IBlogConfig>();
            blogConfigMock.Setup(c => c.AdvancedSettings).Returns(new AdvancedSettings
            {
                RobotsTxtContent = string.Empty
            });

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RobotsTxtMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/robots.txt";

            await middleware.Invoke(ctx, blogConfigMock.Object);

            Assert.AreEqual(StatusCodes.Status404NotFound, ctx.Response.StatusCode);

            Assert.Pass();
        }

        [Test]
        public void TestRobotsTxtMiddlewareExtensions()
        {
            var serviceCollection = new ServiceCollection();
            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());

            applicationBuilder.UseRobotsTxt();

            var app = applicationBuilder.Build();

            Assert.IsInstanceOf<MapWhenMiddleware>(app.Target);
        }
    }
}
