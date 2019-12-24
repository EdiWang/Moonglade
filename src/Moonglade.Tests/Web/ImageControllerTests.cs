using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Model.Settings;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    public class ImageControllerTests
    {
        private Mock<IOptions<AppSettings>> _appSettingsMock;
        private Mock<IOptions<ImageStorageSettings>> _imageStorageSettingsMock;
        private Mock<ILogger<ImageController>> _loggerMock;
        private Mock<IAsyncImageStorageProvider> _asyncImageStorageProviderMock;
        private Mock<IBlogConfig> _blogConfigMock;

        [SetUp]
        public void Setup()
        {
            _appSettingsMock = new Mock<IOptions<AppSettings>>();
            _imageStorageSettingsMock = new Mock<IOptions<ImageStorageSettings>>();
            _imageStorageSettingsMock.Setup(p => p.Value).Returns(new ImageStorageSettings
            {
                CDNSettings = new CDNSettings
                {
                    CDNEndpoint = "https://fake-cdn.edi.wang/images",
                    GetImageByCDNRedirect = true
                }
            });

            _loggerMock = new Mock<ILogger<ImageController>>();
            _asyncImageStorageProviderMock = new Mock<IAsyncImageStorageProvider>();
            _blogConfigMock = new Mock<IBlogConfig>();
        }

        [Test]
        public async Task TestGetImageAsyncCDN()
        {
            const string filename = "test.png";
            var ctl = new ImageController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object);

            var memCacheMock = new Mock<IMemoryCache>();
            var result = await ctl.GetImageAsync(filename, memCacheMock.Object);
            Assert.IsInstanceOf(typeof(RedirectResult), result);
            if (result is RedirectResult rdResult)
            {
                var resultUrl = Utils.CombineUrl(_imageStorageSettingsMock.Object.Value.CDNSettings.CDNEndpoint, filename);
                Assert.That(rdResult.Url, Is.EqualTo(resultUrl));
            }
        }

        [TestCase("<996>.png")]
        [TestCase(":icu.gif")]
        [TestCase("|.jpg")]
        public async Task TestGetImageAsyncInvalidFileNames(string filename)
        {
            var ctl = new ImageController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object);

            var memCacheMock = new Mock<IMemoryCache>();
            var result = await ctl.GetImageAsync(filename, memCacheMock.Object);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }
    }
}
