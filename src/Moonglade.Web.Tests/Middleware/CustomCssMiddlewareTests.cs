using Microsoft.AspNetCore.Http;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CustomCssMiddlewareTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        [Test]
        public void UseCustomCssMiddlewareExtensions()
        {
            var serviceCollection = new ServiceCollection();
            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());

            applicationBuilder.UseCustomCss(options => { });

            var app = applicationBuilder.Build();

            var type = app.Target.GetType();
            Assert.AreEqual(nameof(UseMiddlewareExtensions), type.DeclaringType.Name);
        }

        [Test]
        public async Task Invoke_Disabled()
        {
            _mockBlogConfig.Setup(bc => bc.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings
            {
                EnableCustomCss = false
            });

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/custom.css";

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new CustomCssMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual(404, ctx.Response.StatusCode);
        }

        [Test]
        public async Task Invoke_TooLargeCss()
        {
            _mockBlogConfig.Setup(bc => bc.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings
            {
                EnableCustomCss = true,
                CssCode = new('a', 65536)
            });

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/custom.css";

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new CustomCssMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual(409, ctx.Response.StatusCode);
        }

        [Test]
        public async Task Invoke_InvalidCss()
        {
            _mockBlogConfig.Setup(bc => bc.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings
            {
                EnableCustomCss = true,
                CssCode = "Work 996, Sick ICU!"
            });

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/custom.css";

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new CustomCssMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual(409, ctx.Response.StatusCode);
        }

        [Test]
        public async Task Invoke_ValidCss()
        {
            _mockBlogConfig.Setup(bc => bc.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings
            {
                EnableCustomCss = true,
                CssCode = ".honest-man .hat { color: green !important;}"
            });

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/custom.css";

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new CustomCssMiddleware(RequestDelegate);

            await middleware.Invoke(ctx, _mockBlogConfig.Object);
            Assert.AreEqual(200, ctx.Response.StatusCode);
            Assert.AreEqual("text/css", ctx.Response.ContentType);
        }
    }
}
