using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class HotTagsViewComponentTests
    {
        private MockRepository _mockRepository;

        private Mock<ITagService> _mockTagService;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);

            _mockTagService = _mockRepository.Create<ITagService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private HotTagsViewComponent CreateComponent()
        {
            return new(
                _mockTagService.Object,
                _mockBlogConfig.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings { HotTagAmount = FakeData.Int2 });
            _mockTagService.Setup(p => p.GetHotTagsAsync(It.IsAny<int>())).Throws(new(FakeData.ShortString2));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ContentViewComponentResult>(result);
        }

        [Test]
        public async Task InvokeAsync_View()
        {
            IReadOnlyList<KeyValuePair<Tag, int>> tags = new List<KeyValuePair<Tag, int>>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings { HotTagAmount = FakeData.Int2 });
            _mockTagService.Setup(p => p.GetHotTagsAsync(It.IsAny<int>())).Returns(Task.FromResult(tags));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
        }
    }
}
