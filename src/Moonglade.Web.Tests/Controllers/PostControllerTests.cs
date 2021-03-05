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
    public class PostControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostService> _mockPostService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostService>();
        }

        private PostController CreatePostController()
        {
            return new(_mockPostService.Object);
        }

        [Test]
        public async Task Slug_YearOutOfRange()
        {
            var ctl = CreatePostController();
            var result = await ctl.Slug(DateTime.UtcNow.Year + 1, 9, 9, "6");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }
    }
}
