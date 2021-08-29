using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Core.CategoryFeature;
using Moonglade.Data;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class CategoryControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private CategoryController CreateCategoryController()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task Create_ValidModel()
        {
            var categoryController = CreateCategoryController();
            var model = new EditCategoryRequest
            {
                DisplayName = FakeData.ShortString2,
                RouteName = FakeData.ShortString2,
                Note = FakeData.ShortString1
            };

            var result = await categoryController.Create(model);
            Assert.IsInstanceOf<CreatedResult>(result);
        }

        [Test]
        public async Task Get_NonExists()
        {
            _mockMediator.Setup(c => c.Send(It.IsAny<GetCategoryByIdCommand>(), default))
                .Returns(Task.FromResult((Category)null));

            var categoryController = CreateCategoryController();
            var result = await categoryController.Get(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Get_Exists()
        {
            _mockMediator.Setup(c => c.Send(It.IsAny<GetCategoryByIdCommand>(), default))
                .Returns(Task.FromResult(new Category()));

            var categoryController = CreateCategoryController();
            var result = await categoryController.Get(FakeData.Uid2);

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Update_ValidModel()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<UpdateCategoryCommand>(), default))
                .Returns(Task.FromResult(OperationCode.Done));

            var categoryController = CreateCategoryController();
            var model = new EditCategoryRequest
            {
                DisplayName = FakeData.ShortString2,
                RouteName = FakeData.ShortString2,
                Note = FakeData.ShortString1
            };

            var result = await categoryController.Update(FakeData.Uid1, model);
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task Update_NotFound()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<UpdateCategoryCommand>(), default))
                .Returns(Task.FromResult(OperationCode.ObjectNotFound));

            var categoryController = CreateCategoryController();
            var model = new EditCategoryRequest
            {
                DisplayName = FakeData.ShortString2,
                RouteName = FakeData.ShortString2,
                Note = FakeData.ShortString1
            };

            var result = await categoryController.Update(FakeData.Uid1, model);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Delete_ValidId()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<DeleteCategoryCommand>(), default))
                .Returns(Task.FromResult(OperationCode.Done));

            var categoryController = CreateCategoryController();
            var result = await categoryController.Delete(Guid.NewGuid());
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task Delete_NotFound()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<DeleteCategoryCommand>(), default))
               .Returns(Task.FromResult(OperationCode.ObjectNotFound));

            var categoryController = CreateCategoryController();
            var result = await categoryController.Delete(Guid.NewGuid());
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task List_OK()
        {
            var categoryController = CreateCategoryController();
            var result = await categoryController.List();
            Assert.IsInstanceOf<OkObjectResult>(result);
        }
    }
}
