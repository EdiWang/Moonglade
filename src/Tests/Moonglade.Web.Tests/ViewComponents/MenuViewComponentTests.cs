using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Caching;
using Moonglade.Menus;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class MenuViewComponentTests
    {
        private MockRepository _mockRepository;
        private Mock<IMenuService> _mockMenuService;
        private Mock<IBlogCache> _mockBlogCache;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMenuService = _mockRepository.Create<IMenuService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        private MenuViewComponent CreateComponent()
        {
            return new(_mockMenuService.Object, _mockBlogCache.Object);
        }

        //[Test]
        //public async Task InvokeAsync_Exception()
        //{
        //    _mockMenuService.Setup(p => p.GetAllAsync()).Throws(new(FakeData.ShortString2));

        //    var component = CreateComponent();
        //    var result = await component.InvokeAsync();

        //    Assert.IsInstanceOf<ContentViewComponentResult>(result);
        //}

        [Test]
        public async Task InvokeAsync_View()
        {
            IReadOnlyList<Menu> menus = new List<Menu>();

            _mockMenuService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(menus));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
        }
    }
}
