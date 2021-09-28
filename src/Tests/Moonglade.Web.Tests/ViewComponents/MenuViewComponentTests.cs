using MediatR;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Caching;
using Moonglade.Menus;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class MenuViewComponentTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;
        private Mock<IBlogCache> _mockBlogCache;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        private MenuViewComponent CreateComponent()
        {
            return new(_mockBlogCache.Object, _mockMediator.Object);
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

            _mockMediator.Setup(p => p.Send(It.IsAny<GetAllMenusQuery>(), default)).Returns(Task.FromResult(menus));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
        }
    }
}
