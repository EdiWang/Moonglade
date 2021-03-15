using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Web;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moonglade.Data.Spec;
using Moonglade.Pingback;
using Moonglade.Web.Models;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PostManageControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IPostService> _mockPostService;
        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ITZoneResolver> _mockTZoneResolver;
        private Mock<ILogger<PostManageController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);

            _mockPostService = _mockRepository.Create<IPostService>();
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockTZoneResolver = _mockRepository.Create<ITZoneResolver>();
            _mockLogger = _mockRepository.Create<ILogger<PostManageController>>();
        }

        private PostManageController CreatePostManageController()
        {
            return new(
                _mockPostService.Object,
                _mockCategoryService.Object,
                _mockBlogConfig.Object,
                _mockTZoneResolver.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task ListPublished_Json()
        {
            (IReadOnlyList<PostSegment> Posts, int TotalRows) data = new(new List<PostSegment>(), 996);

            _mockPostService.Setup(p => p.ListSegment(It.IsAny<PostStatus>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(data));

            var postManageController = CreatePostManageController();
            DataTableRequest model = new DataTableRequest
            {
                Draw = 251,
                Length = 35,
                Start = 7,
                Search = new SearchRequest { Value = "996" }
            };

            var result = await postManageController.ListPublished(model);
            Assert.IsInstanceOf<JsonResult>(result);
        }

        [Test]
        public async Task Draft_View()
        {
            (IReadOnlyList<PostSegment> Posts, int TotalRows) data = new(new List<PostSegment>(), 996);

            _mockPostService.Setup(p => p.ListSegment(It.IsAny<PostStatus>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(data));

            var postManageController = CreatePostManageController();
            var result = await postManageController.Draft();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task RecycleBin_View()
        {
            (IReadOnlyList<PostSegment> Posts, int TotalRows) data = new(new List<PostSegment>(), 996);

            _mockPostService.Setup(p => p.ListSegment(It.IsAny<PostStatus>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(data));

            var postManageController = CreatePostManageController();
            var result = await postManageController.RecycleBin();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        //[Test]
        //public async Task Create_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();

        //    // Act
        //    var result = await postManageController.Create();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task Edit_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();
        //    Guid id = default(Guid);

        //    // Act
        //    var result = await postManageController.Edit(
        //        id);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task CreateOrEdit_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();
        //    PostEditModel model = null;
        //    LinkGenerator linkGenerator = null;
        //    IPingbackSender pingbackSender = null;

        //    // Act
        //    var result = await postManageController.CreateOrEdit(
        //        model,
        //        linkGenerator,
        //        pingbackSender);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task Restore_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();
        //    Guid postId = default(Guid);

        //    // Act
        //    var result = await postManageController.Restore(
        //        postId);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task Delete_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();
        //    Guid postId = default(Guid);

        //    // Act
        //    var result = await postManageController.Delete(
        //        postId);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task DeleteFromRecycleBin_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();
        //    Guid postId = default(Guid);

        //    // Act
        //    var result = await postManageController.DeleteFromRecycleBin(
        //        postId);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task EmptyRecycleBin_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();

        //    // Act
        //    var result = await postManageController.EmptyRecycleBin();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task Insights_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var postManageController = CreatePostManageController();

        //    // Act
        //    var result = await postManageController.Insights();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}
    }
}
