using Microsoft.AspNetCore.Http;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using System.IO;
using Moonglade.Web.Models;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ManifestMiddlewareTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IOptions<List<ManifestIcon>>> _mockManifestIcons;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockManifestIcons = _mockRepository.Create<IOptions<List<ManifestIcon>>>();

            _mockBlogConfig.Setup(bc => bc.GeneralSettings).Returns(new GeneralSettings
            {
                SiteTitle = "Fake Title"
            });

            _mockManifestIcons.Setup(p => p.Value).Returns(new List<ManifestIcon>());
        }

        [Test]
        public void UseManifestMiddlewareExtensions()
        {
            var serviceCollection = new ServiceCollection();
            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());

            applicationBuilder.UseManifest(options => { });

            var app = applicationBuilder.Build();

            var type = app.Target.GetType();
            Assert.AreEqual(nameof(UseMiddlewareExtensions), type.DeclaringType.Name);
        }

        [Test]
        public async Task Invoke_OK()
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/manifest.json";

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new ManifestMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object, _mockManifestIcons.Object);
            Assert.AreEqual(200, ctx.Response.StatusCode);
            Assert.AreEqual("application/json", ctx.Response.ContentType);
            Assert.IsNotNull(ctx.Response.Headers["cache-control"]);
        }
    }
}
