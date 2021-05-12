using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Page;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]

    public class BlogPageModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogPageService> _mockBlogPageService;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IOptions<AppSettings>> _mockOptions;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogPageService = _mockRepository.Create<IBlogPageService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockOptions = _mockRepository.Create<IOptions<AppSettings>>();
        }

        private BlogPageModel CreateBlogPageModel()
        {
            return new(
                _mockBlogPageService.Object,
                _mockBlogCache.Object,
                _mockOptions.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task OnGetAsync_EmptySlug(string slug)
        {
            var blogPageModel = CreateBlogPageModel();
            var result = await blogPageModel.OnGetAsync(slug);
            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task OnGetAsync_NotFound_Null()
        {
            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.Page, FakeData.ShortString2, It.IsAny<Func<ICacheEntry, BlogPage>>()))
                .Returns((BlogPage)null);

            var blogPageModel = CreateBlogPageModel();
            var result = await blogPageModel.OnGetAsync(FakeData.ShortString2);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGetAsync_NotFound_Unpublished()
        {
            var page = new BlogPage { IsPublished = false };

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.Page, FakeData.ShortString2, It.IsAny<Func<ICacheEntry, BlogPage>>()))
                .Returns(page);

            var blogPageModel = CreateBlogPageModel();
            var result = await blogPageModel.OnGetAsync(FakeData.ShortString2);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }
    }
}
