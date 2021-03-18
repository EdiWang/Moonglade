using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CategoryModelTests
    {
        private MockRepository _mockRepository;
        private Mock<ICategoryService> _mockCategoryService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
        }

        private CategoryModel CreateCategoryModel()
        {
            return new(_mockCategoryService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new (){Id = Guid.Empty, DisplayName = "Work 996", Note = "Fubao", RouteName = "work-996" }
            };

            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));

            var categoryModel = CreateCategoryModel();
            await categoryModel.OnGet();

            Assert.IsNotNull(categoryModel.Categories);
            Assert.IsNotNull(categoryModel.CategoryEditViewModel);
        }
    }
}
