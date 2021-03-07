using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class OpenSearchMiddlewareTests
    {
        [Test]
        public async Task Invoke_NonOpenSearchRequestPath()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new OpenSearchMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, null);

            Assert.Pass();
        }

        [Test]
        public async Task Invoke_OpenSearchRequestPath()
        {
            Mock<IBlogConfig> blogConfigMock = new();
            blogConfigMock.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
            {
                CanonicalPrefix = "https://996.icu",
                SiteTitle = "996 ICU",
                Description = "Work 996 and get into ICU"
            });

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new OpenSearchMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/opensearch";

            await middleware.Invoke(ctx, blogConfigMock.Object);

            Assert.AreEqual("text/xml", ctx.Response.ContentType);
            Assert.Pass();
        }
    }
}
