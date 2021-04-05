using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Configuration.Settings;
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
        private MockRepository _mockRepository;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IOptions<AppSettings>> _mockOptionsAppSettings;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockOptionsAppSettings = _mockRepository.Create<IOptions<AppSettings>>();
        }

        [Test]
        public async Task Invoke_NonSiteMapRequestPath()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new SiteMapMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, _mockBlogConfig.Object, _mockBlogCache.Object, _mockOptionsAppSettings.Object, null, null);

            Assert.Pass();
        }

        [Test]
        public async Task Invoke_SiteMapRequestPath()
        {
            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
            {
                CanonicalPrefix = "https://996.icu"
            });

            _mockBlogCache
                .Setup(p => p.GetOrCreateAsync(CacheDivision.General, "sitemap",
                    It.IsAny<Func<ICacheEntry, Task<string>>>())).Returns(Task.FromResult("<xml></xml>"));

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new SiteMapMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/sitemap.xml";

            await middleware.Invoke(ctx, _mockBlogConfig.Object, _mockBlogCache.Object, _mockOptionsAppSettings.Object, null, null);

            Assert.AreEqual("text/xml", ctx.Response.ContentType);
            Assert.Pass();
        }
    }
}
