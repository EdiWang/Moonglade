using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Core;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class CategoryListViewComponentTests
    {
        private MockRepository _mockRepository;
        private Mock<ICategoryService> _mockCategoryService;

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
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
        }

        private CategoryListViewComponent CreateComponent()
        {
            return new(_mockCategoryService.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockCategoryService.Setup(p => p.GetAllAsync()).Throws(new(FakeData.ShortString2));

            var component = CreateComponent();
            var result = await component.InvokeAsync(false);

            Assert.IsInstanceOf<ContentViewComponentResult>(result);
        }

        [Test]
        public async Task InvokeAsync_IsMenu()
        {
            _mockCategoryService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(_cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync(true);

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
            Assert.AreEqual("CatMenu", ((ViewViewComponentResult)result).ViewName);
        }

        [Test]
        public async Task InvokeAsync_NotMenu()
        {
            _mockCategoryService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(_cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync(false);

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
            Assert.AreEqual(null, ((ViewViewComponentResult)result).ViewName);
        }
    }
}
