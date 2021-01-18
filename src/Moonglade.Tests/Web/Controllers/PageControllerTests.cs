using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
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

        [Test]
        public async Task Preview_NoPage()
        {
            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult((Page)null));

            var ctl = CreatePageController();
            var result = await ctl.Preview(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Preview_HasPage()
        {
            var fakePage = new Page
            {
                Id = Guid.Empty,
                CreateTimeUtc = new DateTime(996, 9, 6),
                CssContent = ".jack-ma .heart {color: black !important;}",
                HideSidebar = false,
                IsPublished = false,
                MetaDescription = "Fuck Jack Ma",
                RawHtmlContent = "<p>Fuck 996</p>",
                Slug = "fuck-jack-ma",
                Title = "Fuck Jack Ma 1000 years!",
                UpdateTimeUtc = new DateTime(1996, 9, 6)
            };

            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(fakePage));

            var ctl = CreatePageController();
            var result = await ctl.Preview(Guid.Empty);

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<PageViewModel>(model);
            Assert.AreEqual(fakePage.Title, ((PageViewModel)model).Title);
        }
    }
}