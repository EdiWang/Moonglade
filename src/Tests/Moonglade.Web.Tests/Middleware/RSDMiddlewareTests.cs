using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    public class RSDMiddlewareTests
    {
        [Test]
        public async Task Invoke_NonRSDRequestPath()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RSDMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, null);

            Assert.Pass();
        }

        [Test]
        public async Task Invoke_RSDRequestPath()
        {
            Mock<IBlogConfig> blogConfigMock = new();
            blogConfigMock.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
            {
                CanonicalPrefix = FakeData.Url1
            });

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RSDMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/rsd";

            await middleware.Invoke(ctx, blogConfigMock.Object);

            Assert.AreEqual("text/xml", ctx.Response.ContentType);
            Assert.Pass();
        }
    }
}
