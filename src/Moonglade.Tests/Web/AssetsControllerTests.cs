using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using SiteIconGenerator;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AssetsControllerTests
    {
        private Mock<ILogger<AssetsController>> _loggerMock;
        private Mock<IOptions<AppSettings>> _appSettingsMock;
        private Mock<IBlogConfig> _blogConfigMock;
        private Mock<IWebHostEnvironment> _webHostEnvMock;
        private Mock<IOptions<ImageStorageSettings>> _imageStorageSettingsMock;
        private Mock<IBlogImageStorage> _asyncImageStorageProviderMock;
        private Mock<ISiteIconGenerator> _siteIconGeneratorMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new();
            _appSettingsMock = new();
            _blogConfigMock = new();
            _webHostEnvMock = new();
            _asyncImageStorageProviderMock = new();
            _siteIconGeneratorMock = new();
            _imageStorageSettingsMock = new();
            _imageStorageSettingsMock.Setup(p => p.Value).Returns(new ImageStorageSettings
            {
                CDNSettings = new()
                {
                    CDNEndpoint = "https://fake-cdn.edi.wang/images",
                    EnableCDNRedirect = true
                }
            });
        }

        [Test]
        public async Task GetImage_CDN()
        {
            const string filename = "test.png";
            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object,
                _webHostEnvMock.Object);

            var memCacheMock = new Mock<IMemoryCache>();
            var result = await ctl.Image(filename, memCacheMock.Object);
            Assert.IsInstanceOf(typeof(RedirectResult), result);
            if (result is RedirectResult rdResult)
            {
                var resultUrl = _imageStorageSettingsMock.Object.Value.CDNSettings.CDNEndpoint.CombineUrl(filename);
                Assert.That(rdResult.Url, Is.EqualTo(resultUrl));
            }
        }

        [TestCase("<996>.png")]
        [TestCase(":icu.gif")]
        [TestCase("|.jpg")]
        //[Platform(Include = "Win")]
        public async Task GetImage_InvalidFileNames(string filename)
        {
            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object,
                _webHostEnvMock.Object);

            var memCacheMock = new Mock<IMemoryCache>();
            var result = await ctl.Image(filename, memCacheMock.Object);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task Manifest()
        {
            _blogConfigMock.Setup(bc => bc.GeneralSettings).Returns(new Configuration.GeneralSettings
            {
                SiteTitle = "Fake Title"
            });

            _webHostEnvMock.Setup(p => p.WebRootPath).Returns(@"C:\35\404\996\251");
            _appSettingsMock.Setup(p => p.Value).Returns(new AppSettings());

            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object,
                _webHostEnvMock.Object);

            var result = await ctl.Manifest(_webHostEnvMock.Object, null);
            Assert.IsInstanceOf(typeof(JsonResult), result);
            if (result is JsonResult jsonResult)
            {
                if (jsonResult.Value is ManifestModel model)
                {
                    Assert.IsTrue(model.ShortName == _blogConfigMock.Object.GeneralSettings.SiteTitle);
                    Assert.IsTrue(model.Name == _blogConfigMock.Object.GeneralSettings.SiteTitle);
                }
            }
        }
    }
}
