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
using Moonglade.Configuration;
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
        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

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

        [Test]
        public async Task Create_View()
        {
            IReadOnlyList<Category> cats = new List<Category>()
            {
                new(){Id = Guid.Empty, DisplayName = "Work 996", Note = "Get into ICU", RouteName = "work-996"}
            };

            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings()
            {
                DefaultLangCode = "en-US"
            });

            var postManageController = CreatePostManageController();
            var result = await postManageController.Create();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Edit_NotFound()
        {
            _mockPostService.Setup(p => p.GetAsync(Guid.Empty)).Returns(Task.FromResult((Post)null));
            var postManageController = CreatePostManageController();
            var result = await postManageController.Edit(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Edit_View()
        {
            var cat = new Category()
            {
                DisplayName = "WTF",
                Id = Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d"),
                Note = "A wonderful contry",
                RouteName = "wtf"
            };

            var post = new Post
            {
                Id = Uid,
                Title = "Work 996 and Get into ICU",
                Slug = "work-996-and-get-into-icu",
                ContentAbstract = "Get some fubao",
                RawPostContent = "<p>Get some fubao</p>",
                ContentLanguageCode = "en-us",
                Featured = true,
                ExposedToSiteMap = true,
                IsFeedIncluded = true,
                IsPublished = true,
                CommentEnabled = true,
                PubDateUtc = new DateTime(2019, 9, 6, 6, 35, 7),
                LastModifiedUtc = new DateTime(2020, 9, 6, 6, 35, 7),
                CreateTimeUtc = new DateTime(2018, 9, 6, 6, 35, 7),
                Tags = new[]
                {
                    new Tag() { DisplayName = "Fubao", Id = 996, NormalizedName = "fubao" },
                    new Tag() { DisplayName = "996", Id = 251, NormalizedName = "996" }
                },
                Categories = new[] { cat }
            };

            IReadOnlyList<Category> cats = new List<Category>() { cat };

            _mockPostService.Setup(p => p.GetAsync(Uid)).Returns(Task.FromResult(post));
            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));

            var postManageController = CreatePostManageController();
            var result = await postManageController.Edit(Uid);
            Assert.IsInstanceOf<ViewResult>(result);
        }

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

        [Test]
        public async Task Restore_EmptyId()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.Restore(Guid.Empty);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Restore_OK()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.Restore(Uid);
            Assert.IsInstanceOf<OkResult>(result);
        }

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
