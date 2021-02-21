using Moonglade.Core;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CategoryListViewComponentTests
    {
        private MockRepository _mockRepository;
        private Mock<ICategoryService> _mockCategoryService;

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

            var message = ((ViewViewComponentResult) result).ViewData["ComponentErrorMessage"];
            Assert.AreEqual("996", message);
        }
    }
}
