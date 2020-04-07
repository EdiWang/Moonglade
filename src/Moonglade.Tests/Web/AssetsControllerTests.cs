using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Model.Settings;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moonglade.Web.SiteIconGenerator;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    public class AssetsControllerTests
    {
        private Mock<ILogger<AssetsController>> _loggerMock;
        private Mock<IOptions<AppSettings>> _appSettingsMock;
        private Mock<IBlogConfig> _blogConfigMock;
        private Mock<IWebHostEnvironment> _webHostEnvMock;
        private Mock<IOptions<ImageStorageSettings>> _imageStorageSettingsMock;
        private Mock<IAsyncImageStorageProvider> _asyncImageStorageProviderMock;
        private Mock<ISiteIconGenerator> _siteIconGeneratorMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<AssetsController>>();
            _appSettingsMock = new Mock<IOptions<AppSettings>>();
            _blogConfigMock = new Mock<IBlogConfig>();
            _webHostEnvMock = new Mock<IWebHostEnvironment>();
            _asyncImageStorageProviderMock = new Mock<IAsyncImageStorageProvider>();
            _siteIconGeneratorMock = new Mock<ISiteIconGenerator>();
            _imageStorageSettingsMock = new Mock<IOptions<ImageStorageSettings>>();
            _imageStorageSettingsMock.Setup(p => p.Value).Returns(new ImageStorageSettings
            {
                CDNSettings = new CDNSettings
                {
                    CDNEndpoint = "https://fake-cdn.edi.wang/images",
                    GetImageByCDNRedirect = true
                }
            });
        }

        [Test]
        public async Task TestGetImageAsyncCDN()
        {
            const string filename = "test.png";
            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object);

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
            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object);

            var memCacheMock = new Mock<IMemoryCache>();
            var result = await ctl.GetImageAsync(filename, memCacheMock.Object);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public void TestRobotsTxtEmpty()
        {
            _blogConfigMock.Setup(bc => bc.AdvancedSettings).Returns(new Configuration.AdvancedSettings
            {
                RobotsTxtContent = string.Empty
            });

            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object);

            var result = ctl.RobotsTxt();
            Assert.IsInstanceOf(typeof(NotFoundResult), result);
        }

        [Test]
        public void TestRobotsTxtContent()
        {
            _blogConfigMock.Setup(bc => bc.AdvancedSettings).Returns(new Configuration.AdvancedSettings
            {
                RobotsTxtContent = "996"
            });

            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object);

            var result = ctl.RobotsTxt();
            Assert.IsInstanceOf(typeof(ContentResult), result);
        }

        [Test]
        public async Task TestManifest()
        {
            _blogConfigMock.Setup(bc => bc.GeneralSettings).Returns(new Configuration.GeneralSettings
            {
                SiteTitle = "Fake Title"
            });

            _webHostEnvMock.Setup(p => p.WebRootPath).Returns(@"C:\35\404\996\251");

            var ctl = new AssetsController(
                _loggerMock.Object,
                _appSettingsMock.Object,
                _imageStorageSettingsMock.Object,
                _asyncImageStorageProviderMock.Object,
                _blogConfigMock.Object,
                _siteIconGeneratorMock.Object);

            var result = await ctl.Manifest(_webHostEnvMock.Object);
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
