using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class PageControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogPageService> _mockPageService;

        private PageEditModel _pageEditModel;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockPageService = _mockRepository.Create<IBlogPageService>();

            _pageEditModel = new()
            {
                CssContent = ".fubao { color: #996 }",
                HideSidebar = true,
                IsPublished = true,
                MetaDescription = "This is Jack Ma's fubao",
                RawHtmlContent = "<p>Work 996 and Get into ICU</p>",
                Slug = FakeData.Slug2,
                Title = FakeData.Title3
            };
        }

        private PageController CreatePageController()
        {
            return new(
                _mockBlogCache.Object,
                _mockPageService.Object);
        }

        [Test]
        public async Task Delete_Success()
        {
            _mockPageService.Setup(p => p.GetAsync(Guid.Empty)).Returns(Task.FromResult(new BlogPage() { Slug = "996" }));

            var ctl = CreatePageController();
            var result = await ctl.Delete(Guid.Empty);

            Assert.IsInstanceOf<NoContentResult>(result);
            _mockBlogCache.Verify(p => p.Remove(CacheDivision.Page, It.IsAny<string>()));
        }

        [Test]
        public async Task Segment_OK()
        {
            IReadOnlyList<PageSegment> ps = new List<PageSegment>
            {
                new()
                {
                    Id = Guid.Empty,
                    CreateTimeUtc = new(1996,9,9,6,3,5),
                    IsPublished = true,
                    Slug = FakeData.Slug2,
                    Title = FakeData.Title3
                }
            };
            _mockPageService.Setup(p => p.ListSegmentAsync()).Returns(Task.FromResult(ps));

            var ctl = CreatePageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Segment_Null()
        {
            _mockPageService.Setup(p => p.ListSegmentAsync()).Returns(Task.FromResult((IReadOnlyList<PageSegment>)null));

            var ctl = CreatePageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Create_OK()
        {
            var ctl = CreatePageController();

            var result = await ctl.Create(_pageEditModel);
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Edit_OK()
        {
            var ctl = CreatePageController();

            var result = await ctl.Edit(FakeData.Uid2, _pageEditModel);
            Assert.IsInstanceOf<OkObjectResult>(result);
        }
    }
}