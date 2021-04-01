using Microsoft.AspNetCore.Http;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Settings;
using System.Collections.Generic;
using System.Threading;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CaptchaImageMiddlewareTests
    {
        private MockRepository _mockRepository;
        private Mock<IOptions<AppSettings>> _appSettingsOptionsMock;
        private Mock<ISessionBasedCaptcha> _captchaMock;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _appSettingsOptionsMock = _mockRepository.Create<IOptions<AppSettings>>();
            _captchaMock = _mockRepository.Create<ISessionBasedCaptcha>();
        }

        [Test]
        public void UseCaptchaImageMiddlewareExtensions()
        {
            var serviceCollection = new ServiceCollection();
            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());

            applicationBuilder.UseCaptchaImage(options => { });

            var app = applicationBuilder.Build();

            var type = app.Target.GetType();
            Assert.AreEqual(nameof(UseMiddlewareExtensions), type.DeclaringType.Name);
        }

        class FakeSession : ISession
        {
            public bool IsAvailable => true;

            public string Id => "996";

            public IEnumerable<string> Keys => new List<string>();

            public void Clear()
            {

            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task LoadAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
            }

            public void Set(string key, byte[] value)
            {
            }

            public bool TryGetValue(string key, out byte[] value)
            {
                value = null;
                return true;
            }
        }

        [Test]
        public async Task Invoke_OK()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddDistributedMemoryCache();
            serviceCollection.AddSession();
            serviceCollection.AddSessionBasedCaptcha();

            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());
            applicationBuilder.UseSession().UseCaptchaImage(options =>
            {
                options.RequestPath = "/captcha-image";
                options.ImageHeight = 36;
                options.ImageWidth = 100;
            });

            var app = applicationBuilder.Build();

            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/captcha-image");

            var repMock = new Mock<HttpResponse>();
            repMock.Setup(p => p.Body).Returns(new MemoryStream());

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(p => p.Session).Returns(new FakeSession());
            httpContextMock.Setup(p => p.Request).Returns(reqMock.Object);
            httpContextMock.Setup(p => p.Response).Returns(repMock.Object);

            var middleware = new CaptchaImageMiddleware(app);

            _captchaMock.Setup(p =>
                p.GenerateCaptchaImageBytes(It.IsAny<ISession>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Array.Empty<byte>());

            await middleware.Invoke(httpContextMock.Object, _captchaMock.Object);

            _captchaMock.Verify(p => p.GenerateCaptchaImageBytes(It.IsAny<ISession>(), It.IsAny<int>(), It.IsAny<int>()));
        }
    }
}
