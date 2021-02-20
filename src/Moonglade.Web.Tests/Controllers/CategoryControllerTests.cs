using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Data.Entities;
using Moonglade.Web.Models;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CategoryControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<ICategoryService> _mockCategoryService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
        }

        private CategoryController CreateCategoryController()
        {
            return new(_mockCategoryService.Object);
        }

        [Test]
        public async Task Create_BadModelState()
        {
            var categoryController = CreateCategoryController();
            var model = new CategoryEditModel
            {
                DisplayName = "996",
                Note = string.Empty
            };
            categoryController.ModelState.AddModelError("Note", "Note is required");

            var result = await categoryController.Create(model);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Create_ValidModel()
        {
            var categoryController = CreateCategoryController();
            var model = new CategoryEditModel
            {
                DisplayName = "996",
                RouteName = "996",
                Note = "fubao"
            };

            var result = await categoryController.Create(model);
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Edit_Get_NonExists()
        {
            _mockCategoryService.Setup(c => c.Get(It.IsAny<Guid>()))
                .Returns(Task.FromResult((Category)null));

            var categoryController = CreateCategoryController();
            var result = await categoryController.Edit(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Edit_Get_Exists()
        {
            _mockCategoryService.Setup(c => c.Get(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Category()));

            var categoryController = CreateCategoryController();
            var result = await categoryController.Edit(Guid.Empty);

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Edit_ValidModel()
        {
            var categoryController = CreateCategoryController();
            var model = new CategoryEditModel
            {
                DisplayName = "996",
                RouteName = "996",
                Note = "fubao"
            };

            var result = await categoryController.Edit(model);
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Edit_BadModelState()
        {
            var categoryController = CreateCategoryController();
            var model = new CategoryEditModel
            {
                DisplayName = "996",
                Note = string.Empty
            };
            categoryController.ModelState.AddModelError("Note", "Note is required");

            var result = await categoryController.Edit(model);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Delete_EmptyId()
        {
            var categoryController = CreateCategoryController();
            var result = await categoryController.Delete(Guid.Empty);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Delete_ValidId()
        {
            var categoryController = CreateCategoryController();
            var result = await categoryController.Delete(Guid.NewGuid());
            Assert.IsInstanceOf<OkResult>(result);
        }
    }
}
