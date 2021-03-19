using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Spec;
using Moonglade.Pingback;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

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
        private static readonly Category Cat = new()
        {
            DisplayName = "WTF",
            Id = Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d"),
            Note = "A wonderful contry",
            RouteName = "wtf"
        };

        private static readonly Post Post = new()
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
                    new Tag { DisplayName = "Fubao", Id = 996, NormalizedName = "fubao" },
                    new Tag { DisplayName = "996", Id = 251, NormalizedName = "996" }
                },
            Categories = new[] { Cat }
        };

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
            var model = new DataTableRequest
            {
                Draw = 251,
                Length = 35,
                Start = 7,
                Search = new() { Value = "996" }
            };

            var result = await postManageController.ListPublished(model);
            Assert.IsInstanceOf<JsonResult>(result);
        }

        [Test]
        public async Task Create_View()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new(){Id = Guid.Empty, DisplayName = "Work 996", Note = "Get into ICU", RouteName = "work-996"}
            };

            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
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
            IReadOnlyList<Category> cats = new List<Category> { Cat };

            _mockPostService.Setup(p => p.GetAsync(Uid)).Returns(Task.FromResult(Post));
            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));

            var postManageController = CreatePostManageController();
            var result = await postManageController.Edit(Uid);
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task CreateOrEdit_BadModelState()
        {
            var postManageController = CreatePostManageController();
            postManageController.ModelState.AddModelError("", "996");

            PostEditModel model = new();
            Mock<LinkGenerator> mockLinkGenerator = new();
            Mock<IPingbackSender> mockPingbackSender = new();

            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object, mockPingbackSender.Object);

            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        [Test]
        public async Task CreateOrEdit_Exception()
        {
            var postManageController = CreatePostManageController();
            postManageController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            PostEditModel model = new()
            {
                PostId = Guid.Empty,
                Title = Post.Title,
                Slug = Post.Slug,
                EditorContent = Post.RawPostContent,
                LanguageCode = Post.ContentLanguageCode,
                IsPublished = false,
                Featured = true,
                ExposedToSiteMap = true,
                ChangePublishDate = false,
                EnableComment = true,
                FeedIncluded = true,
                Tags = "996,icu",
                CategoryList = new List<CheckBoxViewModel>(),
                SelectedCategoryIds = new[] { Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d") }
            };

            Mock<LinkGenerator> mockLinkGenerator = new();
            Mock<IPingbackSender> mockPingbackSender = new();

            _mockPostService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>())).Throws(new Exception("Work 996"));

            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object, mockPingbackSender.Object);
            Assert.IsInstanceOf<JsonResult>(result);

            var statusCode = postManageController.HttpContext.Response.StatusCode;
            Assert.AreEqual(500, statusCode);

            mockPingbackSender.Verify(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task CreateOrEdit_Create_Draft()
        {
            var postManageController = CreatePostManageController();
            PostEditModel model = new()
            {
                PostId = Guid.Empty,
                Title = Post.Title,
                Slug = Post.Slug,
                EditorContent = Post.RawPostContent,
                LanguageCode = Post.ContentLanguageCode,
                IsPublished = false,
                Featured = true,
                ExposedToSiteMap = true,
                ChangePublishDate = false,
                EnableComment = true,
                FeedIncluded = true,
                Tags = "996,icu",
                CategoryList = new List<CheckBoxViewModel>(),
                SelectedCategoryIds = new[] { Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d") }
            };

            Mock<LinkGenerator> mockLinkGenerator = new();
            Mock<IPingbackSender> mockPingbackSender = new();

            _mockPostService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>())).Returns(Task.FromResult(new PostEntity
            {
                Id = Uid
            }));

            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object, mockPingbackSender.Object);
            Assert.IsInstanceOf<JsonResult>(result);
            mockPingbackSender.Verify(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task CreateOrEdit_Create_Publish_EnablePingback()
        {
            var postManageController = CreatePostManageController();
            postManageController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            PostEditModel model = new()
            {
                PostId = Guid.Empty,
                Title = Post.Title,
                Slug = Post.Slug,
                EditorContent = Post.RawPostContent,
                LanguageCode = Post.ContentLanguageCode,
                IsPublished = true,
                Featured = true,
                ExposedToSiteMap = true,
                ChangePublishDate = false,
                EnableComment = true,
                FeedIncluded = true,
                Tags = "996,icu",
                CategoryList = new List<CheckBoxViewModel>(),
                SelectedCategoryIds = new[] { Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d") }
            };

            Mock<LinkGenerator> mockLinkGenerator = new();
            mockLinkGenerator.Setup(p => p.GetUriByAddress(
                It.IsAny<HttpContext>(),
                It.IsAny<RouteValuesAddress>(),
                It.IsAny<RouteValueDictionary>(),
                It.IsAny<RouteValueDictionary>(),
                It.IsAny<string>(),
                It.IsAny<HostString>(),
                It.IsAny<PathString>(),
                It.IsAny<FragmentString>(),
                It.IsAny<LinkOptions>()
                ))
                .Returns("https://996.icu/1996/7/2/work-996-and-get-into-icu");

            Mock<IPingbackSender> mockPingbackSender = new();
            var trySendPingAsyncCalled = new ManualResetEvent(false);
            mockPingbackSender.Setup(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>())).Callback(() =>
            {
                trySendPingAsyncCalled.Set();
            });

            _mockPostService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>())).Returns(Task.FromResult(new PostEntity
            {
                Id = Uid,
                PubDateUtc = new DateTime(1996, 7, 2, 5, 1, 0),
                ContentAbstract = Post.ContentAbstract
            }));

            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings { EnablePingBackSend = true });

            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object, mockPingbackSender.Object);

            trySendPingAsyncCalled.WaitOne(TimeSpan.FromSeconds(2));
            Assert.IsInstanceOf<JsonResult>(result);

            mockPingbackSender.Verify(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>()));
        }

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

        [Test]
        public async Task Delete_EmptyId()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.Delete(Guid.Empty);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Delete_OK()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.Delete(Uid);
            Assert.IsInstanceOf<OkResult>(result);
            _mockPostService.Verify(p => p.DeleteAsync(It.IsAny<Guid>(), true));
        }

        [Test]
        public async Task DeleteFromRecycleBin_EmptyId()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.DeleteFromRecycleBin(Guid.Empty);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task DeleteFromRecycleBin_OK()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.DeleteFromRecycleBin(Uid);
            Assert.IsInstanceOf<OkResult>(result);
            _mockPostService.Verify(p => p.DeleteAsync(It.IsAny<Guid>(), false));
        }

        [Test]
        public async Task EmptyRecycleBin_View()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.EmptyRecycleBin();

            Assert.IsInstanceOf<RedirectResult>(result);
        }
    }
}
