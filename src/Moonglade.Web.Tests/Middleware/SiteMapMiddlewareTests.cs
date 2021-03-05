using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.BlogProtocols;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SiteMapMiddlewareTests
    {
        [Test]
        public async Task Invoke_NonSiteMapRequestPath()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new SiteMapMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, null, null, null);

            Assert.Pass();
        }

        [Test]
        public async Task Invoke_SiteMapRequestPath()
        {
            Mock<IBlogConfig> blogConfigMock = new();
            Mock<IBlogCache> blogCacheMock = new();
            Mock<ISiteMapWriter> siteMapWriterMock = new();

            blogConfigMock.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
            {
                CanonicalPrefix = "https://996.icu"
            });

            blogCacheMock
                .Setup(p => p.GetOrCreateAsync(CacheDivision.General, "sitemap",
                    It.IsAny<Func<ICacheEntry, Task<string>>>())).Returns(Task.FromResult("<xml></xml>"));

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new SiteMapMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/sitemap.xml";

            await middleware.Invoke(ctx, blogConfigMock.Object, blogCacheMock.Object, siteMapWriterMock.Object);

            Assert.AreEqual("text/xml", ctx.Response.ContentType);
            Assert.Pass();
        }
    }
}
