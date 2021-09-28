using MediatR;
using Moonglade.Core.CategoryFeature;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class CategoryModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private CategoryModel CreateCategoryModel()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new (){Id = Guid.Empty, DisplayName = FakeData.Title3, Note = "Fubao", RouteName = FakeData.Slug2 }
            };

            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Returns(Task.FromResult(cats));

            var categoryModel = CreateCategoryModel();
            await categoryModel.OnGet();

            Assert.IsNotNull(categoryModel.Categories);
            Assert.IsNotNull(categoryModel.EditCategoryRequest);
        }
    }
}
