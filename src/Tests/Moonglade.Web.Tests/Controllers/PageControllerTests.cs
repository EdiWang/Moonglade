using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Pages;
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
        private Mock<ILogger<PageController>> _mockLogger;

        private PageEditModel _pageEditModel;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockPageService = _mockRepository.Create<IBlogPageService>();
            _mockLogger = _mockRepository.Create<ILogger<PageController>>();

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
                _mockPageService.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task Delete_Success()
        {
            _mockPageService.Setup(p => p.GetAsync(Guid.Empty)).Returns(Task.FromResult(new BlogPage() { Slug = "996" }));

            var ctl = CreatePageController();
            var result = await ctl.Delete(Guid.Empty);

            Assert.IsInstanceOf<OkResult>(result);
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
            _mockPageService.Setup(p => p.ListSegment()).Returns(Task.FromResult(ps));

            var ctl = CreatePageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Segment_Error()
        {
            _mockPageService.Setup(p => p.ListSegment()).Returns(Task.FromResult((IReadOnlyList<PageSegment>)null));

            var ctl = CreatePageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<StatusCodeResult>(result);
        }

        [Test]
        public async Task Create_Exception()
        {
            _mockPageService.Setup(p => p.CreateAsync(It.IsAny<UpdatePageRequest>()))
                .Throws(new("Too much fubao"));
            var ctl = CreatePageController();

            var result = await ctl.Create(_pageEditModel);
            Assert.IsInstanceOf<StatusCodeResult>(result);

            Assert.AreEqual(StatusCodes.Status500InternalServerError, ((StatusCodeResult)result).StatusCode);
        }

        [Test]
        public async Task Edit_Exception()
        {
            _mockPageService.Setup(p => p.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdatePageRequest>()))
                .Throws(new("Too much fubao"));
            var ctl = CreatePageController();

            var result = await ctl.Edit(Guid.Empty, _pageEditModel);
            Assert.IsInstanceOf<StatusCodeResult>(result);

            Assert.AreEqual(StatusCodes.Status500InternalServerError, ((StatusCodeResult)result).StatusCode);
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