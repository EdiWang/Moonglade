using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Core.PageFeature;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class PageControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IMediator> _mockMediator;

        private EditPageRequest _editPageRequest;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockMediator = _mockRepository.Create<IMediator>();

            _editPageRequest = new()
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
            return new(_mockBlogCache.Object, _mockMediator.Object);
        }

        [Test]
        public async Task Delete_Success()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetPageByIdQuery>(), default)).Returns(Task.FromResult(new BlogPage() { Slug = "996" }));

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
            _mockMediator.Setup(p => p.Send(new ListPageSegmentQuery(), default)).Returns(Task.FromResult(ps));

            var ctl = CreatePageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Segment_Null()
        {
            _mockMediator.Setup(p => p.Send(new ListPageSegmentQuery(), default)).Returns(Task.FromResult((IReadOnlyList<PageSegment>)null));

            var ctl = CreatePageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Create_OK()
        {
            var ctl = CreatePageController();

            var result = await ctl.Create(_editPageRequest);
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Edit_OK()
        {
            var ctl = CreatePageController();

            var result = await ctl.Edit(FakeData.Uid2, _editPageRequest);
            Assert.IsInstanceOf<OkObjectResult>(result);
        }
    }
}