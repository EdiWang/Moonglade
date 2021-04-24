using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data.Spec;
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

        private Mock<IPostQueryService> _mockPostService;
        private Mock<IPageService> _mockPageService;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ILogger<HomeController>> _mockLogger;
        private Mock<IOptions<AppSettings>> _mockAppSettingsOptions;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockPostService = _mockRepository.Create<IPostQueryService>();
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
                _mockAppSettingsOptions.Object,
                _mockPostService.Object);
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

        [Test]
        public async Task Slug_YearOutOfRange()
        {
            var ctl = CreateHomeController();
            var result = await ctl.Slug(DateTime.UtcNow.Year + 1, 9, 9, "6");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task Slug_EmptySlug(string slug)
        {
            var ctl = CreateHomeController();
            var result = await ctl.Slug(DateTime.UtcNow.Year + 1, 9, 9, slug);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Slug_NullPost()
        {
            _mockPostService.Setup(p => p.GetAsync(It.IsAny<PostSlug>()))
                .Returns(Task.FromResult((Post)null));

            var ctl = CreateHomeController();
            var result = await ctl.Slug(DateTime.UtcNow.Year, 1, 9, "work-996");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Slug_View()
        {
            _mockPostService.Setup(p => p.GetAsync(It.IsAny<PostSlug>()))
                .Returns(Task.FromResult(new Post
                {
                    Id = Guid.Empty,
                    Slug = "work-996",
                    Title = "Work 996"
                }));

            var ctl = CreateHomeController();
            var result = await ctl.Slug(DateTime.UtcNow.Year, 1, 9, "work-996");

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Preview_NullPost()
        {
            _mockPostService.Setup(p => p.GetDraft(It.IsAny<Guid>()))
                .Returns(Task.FromResult((Post)null));

            var ctl = CreateHomeController();
            var result = await ctl.Preview(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Preview_View()
        {
            _mockPostService.Setup(p => p.GetDraft(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Post
                {
                    Id = Guid.Empty,
                    Slug = "work-996",
                    Title = "Work 996"
                }));

            var ctl = CreateHomeController();
            var result = await ctl.Preview(Guid.Parse("e172b031-1c9a-4e4c-b2ea-07469e7b963a"));

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Segment_OK()
        {
            IReadOnlyList<PostSegment> ps = new List<PostSegment>();
            _mockPostService.Setup(p => p.ListSegment(PostStatus.Published)).Returns(Task.FromResult(ps));

            var ctl = CreateHomeController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Segment_Error()
        {
            IReadOnlyList<PostSegment> ps = new List<PostSegment>();
            _mockPostService.Setup(p => p.ListSegment(PostStatus.Published)).Throws(new ArgumentOutOfRangeException("996"));

            var ctl = CreateHomeController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<StatusCodeResult>(result);
        }
    }
}
