using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class TagsControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<ITagService> _mockTagService;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockTagService = _mockRepository.Create<ITagService>();
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private TagsController CreateTagsController()
        {
            return new(_mockTagService.Object, _mockMediator.Object);
        }

        [Test]
        public async Task Names_OK()
        {
            var ctl = CreateTagsController();
            var result = await ctl.Names();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task Create_EmptyName(string name)
        {
            var ctl = CreateTagsController();
            var result = await ctl.Create(name);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [TestCase("(1)")]
        [TestCase("usr/bin")]
        public async Task Create_InvalidName(string name)
        {
            var ctl = CreateTagsController();
            var result = await ctl.Create(name);

            Assert.IsInstanceOf<ConflictResult>(result);
        }

        [Test]
        public async Task Create_OK()
        {
            var ctl = CreateTagsController();
            var result = await ctl.Create(FakeData.ShortString2);

            _mockTagService.Verify(p => p.Create(It.IsAny<string>()));
            Assert.IsInstanceOf<OkResult>(result);
        }

        [Test]
        public async Task Delete_ValidId()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<DeleteTagCommand>(), default))
                .Returns(Task.FromResult(OperationCode.Done));

            var ctl = CreateTagsController();
            var result = await ctl.Delete(FakeData.Int2);

            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task Delete_NotFound()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<DeleteTagCommand>(), default))
                .Returns(Task.FromResult(OperationCode.ObjectNotFound));

            var ctl = CreateTagsController();
            var result = await ctl.Delete(FakeData.Int2);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Update_OK()
        {
            _mockTagService
                .Setup(p => p.UpdateAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(OperationCode.Done));

            var ctl = CreateTagsController();
            var result = await ctl.Update(FakeData.Int2, FakeData.ShortString1);

            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task Update_NotFound()
        {
            _mockTagService
                .Setup(p => p.UpdateAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(OperationCode.ObjectNotFound));

            var ctl = CreateTagsController();
            var result = await ctl.Update(FakeData.Int2, FakeData.ShortString1);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task List_OK()
        {
            var categoryController = CreateTagsController();
            var result = await categoryController.List();
            Assert.IsInstanceOf<OkObjectResult>(result);
        }
    }
}
