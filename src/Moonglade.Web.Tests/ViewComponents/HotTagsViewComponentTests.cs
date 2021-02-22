using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Moonglade.Configuration;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
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
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings { HotTagAmount = 996 });
            _mockTagService.Setup(p => p.GetHotTagsAsync(It.IsAny<int>())).Throws(new("996"));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);

            var message = ((ViewViewComponentResult)result).ViewData["ComponentErrorMessage"];
            Assert.AreEqual("996", message);
        }

        [Test]
        public async Task InvokeAsync_View()
        {
            IReadOnlyList<DegreeTag> tags = new List<DegreeTag>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings { HotTagAmount = 996 });
            _mockTagService.Setup(p => p.GetHotTagsAsync(It.IsAny<int>())).Returns(Task.FromResult(tags));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
        }
    }
}
