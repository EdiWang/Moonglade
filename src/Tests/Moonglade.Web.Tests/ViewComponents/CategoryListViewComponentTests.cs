using MediatR;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Core.CategoryFeature;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class CategoryListViewComponentTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        private readonly IReadOnlyList<Category> _cats = new List<Category>
        {
            new ()
            {
                DisplayName = "Fubao", Id = Guid.Empty, Note = FakeData.Title3, RouteName = FakeData.ShortString2
            }
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private CategoryListViewComponent CreateComponent()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Throws(new(FakeData.ShortString2));

            var component = CreateComponent();
            var result = await component.InvokeAsync(false);

            Assert.IsInstanceOf<ContentViewComponentResult>(result);
        }

        [Test]
        public async Task InvokeAsync_IsMenu()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Returns(Task.FromResult(_cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync(true);

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
            Assert.AreEqual("CatMenu", ((ViewViewComponentResult)result).ViewName);
        }

        [Test]
        public async Task InvokeAsync_NotMenu()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Returns(Task.FromResult(_cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync(false);

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
            Assert.AreEqual(null, ((ViewViewComponentResult)result).ViewName);
        }
    }
}
