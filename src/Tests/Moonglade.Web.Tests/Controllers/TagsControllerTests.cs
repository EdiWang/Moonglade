using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class TagsControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<ITagService> _mockTagService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockTagService = _mockRepository.Create<ITagService>();
        }

        private TagsController CreateTagsController()
        {
            return new(_mockTagService.Object);
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
            var ctl = CreateTagsController();
            var result = await ctl.Delete(FakeData.Int2);

            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task Update_OK()
        {
            var ctl = CreateTagsController();
            var result = await ctl.Update(FakeData.Int2, FakeData.ShortString1);

            Assert.IsInstanceOf<NoContentResult>(result);
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
