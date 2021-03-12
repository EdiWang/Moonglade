using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class DefaultImageMiddlewareTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        [Test]
        public void UseDefaultImageMiddlewareExtensions()
        {
            var serviceCollection = new ServiceCollection();
            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());

            applicationBuilder.UseDefaultImage(options => { });

            var app = applicationBuilder.Build();

            var type = app.Target.GetType();
            Assert.AreEqual(nameof(UseMiddlewareExtensions), type.DeclaringType.Name);
        }

        [Test]
        public async Task Invoke_Not404()
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/996";
            ctx.Request.ContentType = "text/html";
            ctx.Response.StatusCode = 200;

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new DefaultImageMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual("text/html", ctx.Request.ContentType);
        }
    }
}
