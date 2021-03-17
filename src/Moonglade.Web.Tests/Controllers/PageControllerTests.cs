using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Pages;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PageControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IPageService> _mockPageService;
        private Mock<ILogger<PageController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockPageService = _mockRepository.Create<IPageService>();
            _mockLogger = _mockRepository.Create<ILogger<PageController>>();
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
            var ctl = CreatePageController();
            var result = await ctl.Delete(Guid.Empty, "work-996");

            Assert.IsInstanceOf<OkResult>(result);
        }

        [Test]
        public async Task CreateOrEdit_BadModelState()
        {
            var ctl = CreatePageController();
            ctl.ModelState.AddModelError("", "996");

            var result = await ctl.CreateOrEdit(new());
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Segment_OK()
        {
            IReadOnlyList<PageSegment> ps = new List<PageSegment>
            {
                new()
                {
                    Id = Guid.Empty,
                    CreateTimeUtc = new DateTime(1996,9,9,6,3,5),
                    IsPublished = true,
                    Slug = "work-996",
                    Title = "Work 996"
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
    }
}