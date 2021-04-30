using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]

    public class OpenSearchMiddlewareTests
    {
        [Test]
        public void UseCustomCssMiddlewareExtensions()
        {
            var serviceCollection = new ServiceCollection();
            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());

            applicationBuilder.UseOpenSearch(options => { });

            var app = applicationBuilder.Build();

            var type = app.Target.GetType();
            Assert.AreEqual(nameof(UseMiddlewareExtensions), type.DeclaringType.Name);
        }

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
                CanonicalPrefix = FakeData.Url1,
                SiteTitle = "996 ICU",
                Description = FakeData.Title2
            });

            blogConfigMock.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings
            {
                EnableOpenSearch = true
            });

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new OpenSearchMiddleware(RequestDelegate);
            OpenSearchMiddleware.Options.RequestPath = "/opensearch";

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/opensearch";

            await middleware.Invoke(ctx, blogConfigMock.Object);

            Assert.AreEqual("text/xml", ctx.Response.ContentType);
            Assert.Pass();
        }
    }
}
