using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class AssetsControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<AssetsController>> _mockLogger;
        private Mock<IMediator> _mockMediator;
        private Mock<IWebHostEnvironment> _mockWebHostEnv;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<AssetsController>>();
            _mockMediator = _mockRepository.Create<IMediator>();
            _mockWebHostEnv = _mockRepository.Create<IWebHostEnvironment>();
        }

        private AssetsController CreateAssetsController()
        {
            return new(
                _mockLogger.Object,
                _mockMediator.Object,
                _mockWebHostEnv.Object);
        }

        [Test]
        public async Task Avatar_Post_BadData()
        {
            var settingsController = CreateAssetsController();
            string base64Img = "996.icu";

            var result = await settingsController.Avatar(base64Img);
            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        //[Test]
        //public void CustomCss_ValidCss()
        //{
        //    _mockBlogConfig.Setup(bc => bc.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings
        //    {
        //        EnableCustomCss = true,
        //        CssCode = ".honest-man .hat { color: green !important;}"
        //    });

        //    _mockAppSettings.Setup(p => p.Value).Returns(new AppSettings());

        //    var ctl = CreateAssetsController();

        //    var result = ctl.CustomCss();
        //    Assert.IsInstanceOf(typeof(ContentResult), result);

        //    var content = (ContentResult)result;
        //    Assert.AreEqual("text/css", content.ContentType);
        //}
    }
}
