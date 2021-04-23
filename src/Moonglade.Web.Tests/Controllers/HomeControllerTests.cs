using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Pages;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class HomeControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IPageService> _mockPageService;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ILogger<HomeController>> _mockLogger;
        private Mock<IOptions<AppSettings>> _mockAppSettingsOptions;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockPageService = _mockRepository.Create<IPageService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockLogger = _mockRepository.Create<ILogger<HomeController>>();
            _mockAppSettingsOptions = _mockRepository.Create<IOptions<AppSettings>>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        private HomeController CreateHomeController()
        {
            return new(
                _mockPageService.Object,
                _mockBlogCache.Object,
                _mockLogger.Object,
                _mockAppSettingsOptions.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task Page_EmptySlug(string slug)
        {
            var ctl = CreateHomeController();
            var result = await ctl.Page(slug);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task Page_NotFound_Null()
        {
            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.Page, "996", It.IsAny<Func<ICacheEntry, BlogPage>>()))
                .Returns((BlogPage)null);

            var ctl = CreateHomeController();
            var result = await ctl.Page("996");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Page_NotFound_Unpublished()
        {
            var page = new BlogPage { IsPublished = false };

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.Page, "996", It.IsAny<Func<ICacheEntry, BlogPage>>()))
                .Returns(page);

            var ctl = CreateHomeController();
            var result = await ctl.Page("996");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetLanguage_EmptyCulture(string culture)
        {
            var ctl = CreateHomeController();
            var result = ctl.SetLanguage(culture, null);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public void SetLanguage_Cookie()
        {
            var ctl = CreateHomeController();
            var result = ctl.SetLanguage("en-US", "/996/icu");

            Assert.IsInstanceOf<LocalRedirectResult>(result);
        }
    }
}
