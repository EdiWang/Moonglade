using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MemoryCache.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.ImageStorage;
using Moonglade.Utils;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class ImageControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogImageStorage> _mockBlogImageStorage;
        private Mock<ILogger<ImageController>> _mockLogger;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IOptions<AppSettings>> _mockAppSettings;
        private Mock<IOptions<ImageStorageSettings>> _mockImageStorageSettings;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogImageStorage = _mockRepository.Create<IBlogImageStorage>();
            _mockLogger = _mockRepository.Create<ILogger<ImageController>>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockAppSettings = _mockRepository.Create<IOptions<AppSettings>>();
            _mockImageStorageSettings = _mockRepository.Create<IOptions<ImageStorageSettings>>();
        }

        private ImageController CreateImageController()
        {
            return new(
                _mockBlogImageStorage.Object,
                _mockLogger.Object,
                _mockBlogConfig.Object,
                _mockAppSettings.Object,
                _mockImageStorageSettings.Object);
        }

        [TestCase("<996>.png")]
        [TestCase(":icu.gif")]
        [TestCase("|.jpg")]
        [Platform(Include = "Win")]
        public async Task GetImage_InvalidFileNames(string filename)
        {
            var ctl = CreateImageController();

            var memCacheMock = new Mock<IMemoryCache>();
            var result = await ctl.Image(filename, memCacheMock.Object);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task GetImage_CDN()
        {
            const string filename = "test.png";

            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings()
            {
                EnableCDNRedirect = true,
                CDNEndpoint = "https://cdn.996.icu/fubao"
            });

            var ctl = CreateImageController();

            var memCacheMock = new Mock<IMemoryCache>();
            var result = await ctl.Image(filename, memCacheMock.Object);
            Assert.IsInstanceOf(typeof(RedirectResult), result);
            if (result is RedirectResult rdResult)
            {
                var resultUrl = _mockBlogConfig.Object.AdvancedSettings.CDNEndpoint.CombineUrl(filename);
                Assert.That(rdResult.Url, Is.EqualTo(resultUrl));
            }
        }

        [Test]
        public async Task Image_Null()
        {
            const string filename = "test.png";

            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings()
            {
                EnableCDNRedirect = false,
            });

            _mockAppSettings.Setup(p => p.Value).Returns(new AppSettings
            {
                CacheSlidingExpirationMinutes = new Dictionary<string, int>
                {
                    { "Image", FakeData.Int2 }
                }
            });

            var memCacheMock = Create.MockedMemoryCache();
            _mockBlogImageStorage.Setup(p => p.GetAsync(It.IsAny<string>()))
                .Returns(Task.FromResult((ImageInfo)null));

            var ctl = CreateImageController();
            var result = await ctl.Image(filename, memCacheMock);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Image_File()
        {
            const string filename = "test.png";

            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings()
            {
                EnableCDNRedirect = false,
            });

            _mockAppSettings.Setup(p => p.Value).Returns(new AppSettings
            {
                CacheSlidingExpirationMinutes = new Dictionary<string, int>
                {
                    { "Image", FakeData.Int2 }
                }
            });

            var memCacheMock = Create.MockedMemoryCache();
            _mockBlogImageStorage.Setup(p => p.GetAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ImageInfo
                {
                    ImageBytes = Array.Empty<byte>(),
                    ImageExtensionName = ".png"
                }));

            var ctl = CreateImageController();
            var result = await ctl.Image(filename, memCacheMock);

            Assert.IsInstanceOf<FileContentResult>(result);
        }

        [Test]
        public async Task Image_Upload_NullFile()
        {
            var ctl = CreateImageController();
            var result = await ctl.Image((IFormFile)null, null);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task Image_Upload_InvalidExtension()
        {
            _mockImageStorageSettings.Setup(p => p.Value).Returns(new ImageStorageSettings
            {
                AllowedExtensions = new[] { ".png" }
            });

            IFormFile file = new FormFile(new MemoryStream(), 0, 1024, "996.jpg", "996.jpg");

            var ctl = CreateImageController();
            var result = await ctl.Image(file, null);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }
    }
}
