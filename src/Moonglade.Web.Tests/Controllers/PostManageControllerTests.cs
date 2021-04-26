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

        private Mock<IPostQueryService> _mockPostService;
        private Mock<IPostManageService> _mockPostManageService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ITimeZoneResolver> _mockTZoneResolver;
        private Mock<IPingbackSender> _mockPingbackSender;
        private Mock<ILogger<PostManageController>> _mockLogger;

        private static readonly Category Cat = new()
        {
            DisplayName = "WTF",
            Id = Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d"),
            Note = "A wonderful contry",
            RouteName = "wtf"
        };

        private static readonly Post Post = new()
        {
            Id = FakeData.Uid1,
            Title = FakeData.Title2,
            Slug = FakeData.Slug1,
            ContentAbstract = "Get some fubao",
            RawPostContent = "<p>Get some fubao</p>",
            ContentLanguageCode = "en-us",
            Featured = true,
            ExposedToSiteMap = true,
            IsFeedIncluded = true,
            IsPublished = true,
            CommentEnabled = true,
            PubDateUtc = new(2019, 9, 6, 6, 35, 7),
            LastModifiedUtc = new(2020, 9, 6, 6, 35, 7),
            CreateTimeUtc = new(2018, 9, 6, 6, 35, 7),
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
            _mockRepository = new(MockBehavior.Default);

            _mockPostService = _mockRepository.Create<IPostQueryService>();
            _mockPostManageService = _mockRepository.Create<IPostManageService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockTZoneResolver = _mockRepository.Create<ITimeZoneResolver>();
            _mockPingbackSender = _mockRepository.Create<IPingbackSender>();
            _mockLogger = _mockRepository.Create<ILogger<PostManageController>>();
        }

        private PostManageController CreatePostManageController()
        {
            return new(
                _mockPostService.Object,
                _mockPostManageService.Object,
                _mockBlogConfig.Object,
                _mockTZoneResolver.Object,
                _mockPingbackSender.Object,
                _mockLogger.Object);
        }

        [Test]
        public void KeepAlive()
        {
            var ctl = CreatePostManageController();
            var result = ctl.KeepAlive("996.ICU");
            Assert.IsInstanceOf(typeof(OkObjectResult), result);
        }

        [Test]
        public async Task Segment_OK()
        {
            IReadOnlyList<PostSegment> ps = new List<PostSegment>();
            _mockPostService.Setup(p => p.ListSegment(PostStatus.Published)).Returns(Task.FromResult(ps));

            var ctl = CreatePostManageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Segment_Error()
        {
            IReadOnlyList<PostSegment> ps = new List<PostSegment>();
            _mockPostService.Setup(p => p.ListSegment(PostStatus.Published)).Throws(new ArgumentOutOfRangeException("996"));

            var ctl = CreatePostManageController();
            var result = await ctl.Segment();

            Assert.IsInstanceOf<StatusCodeResult>(result);
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
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task CreateOrEdit_BadModelState()
        {
            var postManageController = CreatePostManageController();
            postManageController.ModelState.AddModelError("", "996");

            MagicWrapper<PostEditModel> model = new();
            Mock<LinkGenerator> mockLinkGenerator = new();
            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object);

            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        [Test]
        public async Task CreateOrEdit_Exception()
        {
            var postManageController = CreatePostManageController();
            postManageController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            MagicWrapper<PostEditModel> model = new()
            {
                ViewModel = new()
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
                }
            };

            Mock<LinkGenerator> mockLinkGenerator = new();

            _mockPostManageService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>())).Throws(new("Work 996"));

            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object);
            Assert.IsInstanceOf<ConflictObjectResult>(result);

            var statusCode = postManageController.HttpContext.Response.StatusCode;
            Assert.AreEqual(500, statusCode);

            _mockPingbackSender.Verify(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task CreateOrEdit_Create_Draft()
        {
            var postManageController = CreatePostManageController();
            MagicWrapper<PostEditModel> model = new()
            {
                ViewModel = new()
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
                }
            };

            Mock<LinkGenerator> mockLinkGenerator = new();
            _mockPostManageService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>())).Returns(Task.FromResult(new PostEntity
            {
                Id = FakeData.Uid1
            }));

            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object);
            Assert.IsInstanceOf<OkObjectResult>(result);
            _mockPingbackSender.Verify(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task CreateOrEdit_Create_Publish_EnablePingback()
        {
            var postManageController = CreatePostManageController();
            postManageController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            MagicWrapper<PostEditModel> model = new()
            {
                ViewModel = new()
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
                }
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

            var trySendPingAsyncCalled = new ManualResetEvent(false);
            _mockPingbackSender.Setup(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>())).Callback(() =>
            {
                trySendPingAsyncCalled.Set();
            });

            _mockPostManageService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>())).Returns(Task.FromResult(new PostEntity
            {
                Id = FakeData.Uid1,
                PubDateUtc = new(1996, 7, 2, 5, 1, 0),
                ContentAbstract = Post.ContentAbstract
            }));

            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings { EnablePingBackSend = true });

            var result = await postManageController.CreateOrEdit(model, mockLinkGenerator.Object);

            trySendPingAsyncCalled.WaitOne(TimeSpan.FromSeconds(2));
            Assert.IsInstanceOf<OkObjectResult>(result);

            _mockPingbackSender.Verify(p => p.TrySendPingAsync(It.IsAny<string>(), It.IsAny<string>()));
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
            var result = await postManageController.Restore(FakeData.Uid1);
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
            var result = await postManageController.Delete(FakeData.Uid1);
            Assert.IsInstanceOf<OkResult>(result);
            _mockPostManageService.Verify(p => p.DeleteAsync(It.IsAny<Guid>(), true));
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
            var result = await postManageController.DeleteFromRecycleBin(FakeData.Uid1);
            Assert.IsInstanceOf<OkResult>(result);
            _mockPostManageService.Verify(p => p.DeleteAsync(It.IsAny<Guid>(), false));
        }

        [Test]
        public async Task EmptyRecycleBin_View()
        {
            var postManageController = CreatePostManageController();
            var result = await postManageController.EmptyRecycleBin();

            Assert.IsInstanceOf<RedirectToPageResult>(result);
        }
    }
}
