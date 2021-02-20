using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
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

        [TestCase(-1)]
        [TestCase(0)]
        public async Task Delete_InvalidId(int tagId)
        {
            var ctl = CreateTagsController();
            var result = await ctl.Delete(tagId);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
    }
}
