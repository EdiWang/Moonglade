using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moonglade.Core;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CategoryListViewComponentTests
    {
        private MockRepository _mockRepository;
        private Mock<ICategoryService> _mockCategoryService;

        private readonly IReadOnlyList<Category> _cats = new List<Category>
        {
            new ()
            {
                DisplayName = "Fubao", Id = Guid.Empty, Note = "Work 996", RouteName = "996"
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
            _mockCategoryService.Setup(p => p.GetAll()).Throws(new("996"));

            var component = CreateComponent();
            var result = await component.InvokeAsync(false);

            Assert.IsInstanceOf<ViewViewComponentResult>(result);

            var message = ((ViewViewComponentResult)result).ViewData["ComponentErrorMessage"];
            Assert.AreEqual("996", message);
        }

        [Test]
        public async Task InvokeAsync_IsMenu()
        {
            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(_cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync(true);

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
            Assert.AreEqual("CatMenu", ((ViewViewComponentResult)result).ViewName);
        }

        [Test]
        public async Task InvokeAsync_NotMenu()
        {
            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(_cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync(false);

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
            Assert.AreEqual(null, ((ViewViewComponentResult)result).ViewName);
        }
    }
}
