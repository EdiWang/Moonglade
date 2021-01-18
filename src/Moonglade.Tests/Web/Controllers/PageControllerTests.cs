using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Core;
using Moonglade.Model.Settings;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web.Controllers
{
    [TestFixture]
    public class PageControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IOptions<AppSettings>> _mockOptions;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IPageService> _mockPageService;
        private Mock<ILogger<PageController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockOptions = _mockRepository.Create<IOptions<AppSettings>>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockPageService = _mockRepository.Create<IPageService>();
            _mockLogger = _mockRepository.Create<ILogger<PageController>>();
        }

        private PageController CreatePageController()
        {
            return new(
                _mockOptions.Object,
                _mockBlogCache.Object,
                _mockPageService.Object,
                _mockLogger.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task Index_EmptySlug(string slug)
        {
            var ctl = CreatePageController();
            var result = await ctl.Index(slug);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public void Create_Success()
        {
            var ctl = CreatePageController();
            var result = ctl.Create();

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsInstanceOf<PageEditViewModel>(((ViewResult)result).Model);
        }

        [Test]
        public async Task Delete_Success()
        {
            var ctl = CreatePageController();
            var result = await ctl.Delete(Guid.Empty, "work-996");

            Assert.IsInstanceOf<OkResult>(result);
        }
    }
}
