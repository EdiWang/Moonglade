using MediatR;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Configuration;
using Moonglade.Core.TagFeature;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class HotTagsViewComponentTests
    {
        private MockRepository _mockRepository;

        private Mock<IMediator> _mockMediator;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);

            _mockMediator = _mockRepository.Create<IMediator>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private HotTagsViewComponent CreateComponent()
        {
            return new(_mockBlogConfig.Object, _mockMediator.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings { HotTagAmount = FakeData.Int2 });
            _mockMediator.Setup(p => p.Send(It.IsAny<GetHotTagsQuery>(), default)).Throws(new(FakeData.ShortString2));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ContentViewComponentResult>(result);
        }

        [Test]
        public async Task InvokeAsync_View()
        {
            IReadOnlyList<KeyValuePair<Tag, int>> tags = new List<KeyValuePair<Tag, int>>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings { HotTagAmount = FakeData.Int2 });
            _mockMediator.Setup(p => p.Send(It.IsAny<GetHotTagsQuery>(), default)).Returns(Task.FromResult(tags));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
        }
    }
}
