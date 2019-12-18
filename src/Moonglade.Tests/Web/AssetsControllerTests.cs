using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model.Settings;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
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

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<AssetsController>>();
            _appSettingsMock = new Mock<IOptions<AppSettings>>();
            _blogConfigMock = new Mock<IBlogConfig>();
        }

        [Test]
        public void TestManifest()
        {
            _blogConfigMock.Setup(bc => bc.GeneralSettings).Returns(new Configuration.GeneralSettings
            {
                SiteTitle = "Fake Title"
            });

            var ctl = new AssetsController(_loggerMock.Object, _appSettingsMock.Object, _blogConfigMock.Object);
            var result = ctl.Manifest();
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
