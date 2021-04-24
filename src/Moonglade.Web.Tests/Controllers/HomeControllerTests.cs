using System.Diagnostics.CodeAnalysis;
using Moonglade.Configuration;
using Moonglade.Core;
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
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostQueryService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        //[Test]
        //public async Task Preview_NullPost()
        //{
        //    _mockPostService.Setup(p => p.GetDraft(It.IsAny<Guid>()))
        //        .Returns(Task.FromResult((Post)null));

        //    var ctl = CreateHomeController();
        //    var result = await ctl.Preview(Guid.Empty);

        //    Assert.IsInstanceOf<NotFoundResult>(result);
        //}

        //[Test]
        //public async Task Preview_View()
        //{
        //    _mockPostService.Setup(p => p.GetDraft(It.IsAny<Guid>()))
        //        .Returns(Task.FromResult(new Post
        //        {
        //            Id = Guid.Empty,
        //            Slug = "work-996",
        //            Title = "Work 996"
        //        }));

        //    var ctl = CreateHomeController();
        //    var result = await ctl.Preview(Guid.Parse("e172b031-1c9a-4e4c-b2ea-07469e7b963a"));

        //    Assert.IsInstanceOf<ViewResult>(result);
        //}
    }
}
