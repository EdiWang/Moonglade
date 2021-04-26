using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.ImageStorage;
using Moonglade.Pages;
using Moonglade.Utils;
using Moq;
using NUnit.Framework;
using WilderMinds.MetaWeblog;
using Post = Moonglade.Core.Post;
using Tag = Moonglade.Core.Tag;

namespace Moonglade.Web.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MetaWeblogServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ITimeZoneResolver> _mockTZoneResolver;
        private Mock<ILogger<MetaWeblogService>> _mockLogger;
        private Mock<ITagService> _mockTagService;
        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IPostQueryService> _mockPostService;
        private Mock<IPostManageService> _mockPostManageService;
        private Mock<IBlogPageService> _mockPageService;
        private Mock<IBlogImageStorage> _mockBlogImageStorage;
        private Mock<IFileNameGenerator> _mockFileNameGenerator;

        private readonly string _key = "work996andgetintoicu";
        private readonly string _username = "moonglade";
        private readonly string _password = "work996";
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
                new Tag {DisplayName = "Fubao", Id = 996, NormalizedName = FakeData.ShortString1},
                new Tag {DisplayName = FakeData.ShortString2, Id = FakeData.Int1, NormalizedName = FakeData.ShortString2}
            },
            Categories = new[] { Cat }
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockTZoneResolver = _mockRepository.Create<ITimeZoneResolver>();
            _mockLogger = _mockRepository.Create<ILogger<MetaWeblogService>>();
            _mockTagService = _mockRepository.Create<ITagService>();
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockPostService = _mockRepository.Create<IPostQueryService>();
            _mockPostManageService = _mockRepository.Create<IPostManageService>();
            _mockPageService = _mockRepository.Create<IBlogPageService>();
            _mockBlogImageStorage = _mockRepository.Create<IBlogImageStorage>();
            _mockFileNameGenerator = _mockRepository.Create<IFileNameGenerator>();

            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
            {
                OwnerEmail = "fubao@996.icu",
                OwnerName = "996 Worker",
                CanonicalPrefix = FakeData.Url1,
                SiteTitle = "996 ICU"
            });

            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings()
            {
                MetaWeblogPasswordHash = Helper.HashPassword(_password)
            });
        }

        private MetaWeblogService CreateService()
        {
            return new(
                _mockBlogConfig.Object,
                _mockTZoneResolver.Object,
                _mockLogger.Object,
                _mockTagService.Object,
                _mockCategoryService.Object,
                _mockPostService.Object,
                _mockPostManageService.Object,
                _mockPageService.Object,
                _mockBlogImageStorage.Object,
                _mockFileNameGenerator.Object);
        }

        [Test]
        public async Task GetUserInfoAsync_ExpectedBehavior()
        {
            var service = CreateService();

            var result = await service.GetUserInfoAsync(_key, _username, _password);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GetUsersBlogsAsync_ExpectedBehavior()
        {
            var service = CreateService();
            var result = await service.GetUsersBlogsAsync(_key, _username, _password);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GetPostAsync_ExpectedBehavior()
        {
            _mockPostService.Setup(p => p.GetAsync(It.IsAny<Guid>())).Returns(Task.FromResult(Post));

            var service = CreateService();
            var result = await service.GetPostAsync(Uid.ToString(), _username, _password);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GetRecentPostsAsync_ExpectedBehavior()
        {
            var service = CreateService();
            var result = await service.GetRecentPostsAsync("996.icu", _username, _password, 996);
            Assert.IsNotNull(result);
        }

        [Test]
        public void GetRecentPostsAsync_InvalidParameter()
        {
            var service = CreateService();
            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.GetRecentPostsAsync("996.icu", _username, _password, -1);
            });
        }

        [Test]
        public async Task AddPostAsync_OK()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                Cat
            };

            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));
            _mockPostManageService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>()))
                .Returns(Task.FromResult(new PostEntity { Id = Uid }));

            var service = CreateService();
            await service.AddPostAsync("996.icu", _username, _password, new()
            {
                title = Post.Title,
                categories = new[] { Cat.DisplayName },
                wp_slug = Post.Slug,
                description = Post.RawPostContent,
                mt_keywords = "996,icu"
            }, true);

            _mockPostManageService.Verify(p => p.CreateAsync(It.IsAny<UpdatePostRequest>()));
        }

        [Test]
        public async Task DeletePostAsync_OK()
        {
            var service = CreateService();
            await service.DeletePostAsync("996.icu", Uid.ToString(), _username, _password, true);

            _mockPostManageService.Verify(p => p.DeleteAsync(It.IsAny<Guid>(), true));
        }
    }
}
