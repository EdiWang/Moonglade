using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
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
            ctx.Response.StatusCode = StatusCodes.Status200OK;

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new DefaultImageMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual("text/html", ctx.Request.ContentType);
        }

        [Test]
        public async Task Invoke_404ButNotImagePath()
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/icu";
            ctx.Request.ContentType = "image/pnghahaha";
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new DefaultImageMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual("image/pnghahaha", ctx.Request.ContentType);
        }

        [Test]
        public async Task Invoke_404ImagePathButNoUseFriendlyNotFoundImage()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings()
            {
                UseFriendlyNotFoundImage = false
            });

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/image/996icu.png";
            ctx.Request.ContentType = "image/pnghahahaha";
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new DefaultImageMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual("image/pnghahahaha", ctx.Request.ContentType);
        }

        [Test]
        public async Task Invoke_404ImageEnabled_ExtensionNotAllowed()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings()
            {
                UseFriendlyNotFoundImage = true
            });

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/image/996icu.jpeg";
            ctx.Request.ContentType = "image/jpeghahahaha";
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new DefaultImageMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual("image/jpeghahahaha", ctx.Request.ContentType);
        }

        [Test]
        public async Task Invoke_404ImageEnabled_SetDefault_FileNotFound()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings()
            {
                UseFriendlyNotFoundImage = true
            });

            DefaultImageMiddleware.Options = new DefaultImageMiddlewareOptions
            {
                AllowedExtensions = new [] { ".png" },
                DefaultImagePath = "/fuck996.png"
            };

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/image/996icu.png";
            ctx.Request.ContentType = "image/png";
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new DefaultImageMiddleware(RequestDelegate);
            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual("image/png", ctx.Request.ContentType);
        }
    }
}
