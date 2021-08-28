using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.ImageStorage;
using Moonglade.Utils;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WilderMinds.MetaWeblog;
using Post = Moonglade.Core.Post;
using Tag = Moonglade.Core.Tag;

namespace Moonglade.Web.Tests
{
    [TestFixture]

    public class MetaWeblogServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ITimeZoneResolver> _mockTZoneResolver;
        private Mock<ILogger<MetaWeblogService>> _mockLogger;
        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IPostQueryService> _mockPostService;
        private Mock<IPostManageService> _mockPostManageService;
        private Mock<IBlogImageStorage> _mockBlogImageStorage;
        private Mock<IFileNameGenerator> _mockFileNameGenerator;
        private Mock<IMediator> _mockMediator;

        private readonly string _key = "work996andgetintoicu";
        private readonly string _username = "moonglade";
        private readonly string _password = "work996";
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
                new Tag {DisplayName = "Fubao", Id = 996, NormalizedName = FakeData.ShortString1},
                new Tag {DisplayName = FakeData.ShortString2, Id = FakeData.Int1, NormalizedName = FakeData.ShortString2}
            },
            Categories = new[] { Cat }
        };

        [SetUp]
        public void SetUp()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockTZoneResolver = _mockRepository.Create<ITimeZoneResolver>();
            _mockLogger = _mockRepository.Create<ILogger<MetaWeblogService>>();
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockPostService = _mockRepository.Create<IPostQueryService>();
            _mockPostManageService = _mockRepository.Create<IPostManageService>();
            _mockBlogImageStorage = _mockRepository.Create<IBlogImageStorage>();
            _mockFileNameGenerator = _mockRepository.Create<IFileNameGenerator>();
            _mockMediator = _mockRepository.Create<IMediator>();

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
                _mockCategoryService.Object,
                _mockPostService.Object,
                _mockPostManageService.Object,
                _mockBlogImageStorage.Object,
                _mockFileNameGenerator.Object, _mockMediator.Object);
        }

        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase(null, " ")]
        [TestCase("", null)]
        [TestCase(" ", null)]
        public void EnsureUser_EmptyParameters(string username, string password)
        {
            var service = CreateService();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await service.GetUserInfoAsync(_key, username, password);
            });
        }

        [TestCase("admin", "123456")]
        [TestCase("moonglade", "123456")]
        public void EnsureUser_BadCredential(string username, string password)
        {
            var service = CreateService();

            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.GetUserInfoAsync(_key, username, password);
            });
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
            var result = await service.GetPostAsync(FakeData.Uid1.ToString(), _username, _password);
            Assert.IsNotNull(result);
        }

        [Test]
        public void GetPostAsync_InvliadId()
        {
            var service = CreateService();
            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.GetPostAsync("996", _username, _password);
            });
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

            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Returns(Task.FromResult(cats));
            _mockPostManageService.Setup(p => p.CreateAsync(It.IsAny<UpdatePostRequest>()))
                .Returns(Task.FromResult(new PostEntity { Id = FakeData.Uid1 }));

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
        public async Task EditPostAsync_OK()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                Cat
            };
            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Returns(Task.FromResult(cats));

            var service = CreateService();
            var result = await service.EditPostAsync(FakeData.Uid1.ToString(), _username, _password, new()
            {
                title = Post.Title,
                categories = new[] { Cat.DisplayName },
                wp_slug = Post.Slug,
                description = Post.RawPostContent,
                mt_keywords = "996,icu"
            }, true);

            Assert.IsTrue(result);
            _mockPostManageService.Verify(p => p.UpdateAsync(FakeData.Uid1, It.IsAny<UpdatePostRequest>()));
        }

        [Test]
        public async Task GetCategoriesAsync_OK()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                Cat
            };
            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Returns(Task.FromResult(cats));

            var service = CreateService();
            var result = await service.GetCategoriesAsync("996.icu", _username, _password);

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task AddCategoryAsync_OK()
        {
            var cat = new NewCategory
            {
                name = "996",
                description = "work 996",
                slug = "work-996"
            };

            var service = CreateService();
            var result = await service.AddCategoryAsync("996.icu", _username, _password, cat);

            Assert.AreEqual(996, result);
            _mockCategoryService.Verify(p => p.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [Test]
        public async Task GetTagsAsync_OK()
        {
            IReadOnlyList<string> names = new[] { "996", "icu" };
            _mockMediator.Setup(p => p.Send(It.IsAny<GetTagNamesQuery>(), default)).Returns(Task.FromResult(names));

            var service = CreateService();
            var result = await service.GetTagsAsync("996.icu", _username, _password);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Length);
        }

        [Test]
        public async Task NewMediaObjectAsync_OK()
        {
            _mockFileNameGenerator.Setup(p => p.GetFileName(It.IsAny<string>(), string.Empty))
                .Returns("img-425c62b3-4ecc-49ca-95d1-9ddef6e21725.png");

            _mockBlogImageStorage.Setup(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Returns(Task.FromResult("img-425c62b3-4ecc-49ca-95d1-9ddef6e21725.png"));

            var mo = new MediaObject
            {
                name = "996.png",
                type = "image/png",
                bits = FakeData.ImageBase64
            };

            var service = CreateService();
            var result = await service.NewMediaObjectAsync("996.icu", _username, _password, mo);

            Assert.IsNotNull(result);
            Assert.AreEqual($"https://996.icu/image/img-425c62b3-4ecc-49ca-95d1-9ddef6e21725.png", result.url);
        }

        [Test]
        public void GetPageAsync_InvalidId()
        {
            var service = CreateService();
            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.GetPageAsync("996.icu", "996", _username, _password);
            });
        }

        [Test]
        public async Task GetPageAsync_OK()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetPageByIdQuery>(), default)).Returns(Task.FromResult(FakeData.FakePage));
            var service = CreateService();

            var result = await service.GetPageAsync("996.icu", FakeData.Uid2.ToString(), _username, _password);

            Assert.IsNotNull(result);
            Assert.AreEqual(FakeData.FakePage.Title, result.title);
        }

        [Test]
        public async Task GetPagesAsync_OK()
        {
            IReadOnlyList<BlogPage> pages = new List<BlogPage> { FakeData.FakePage };
            _mockMediator.Setup(p => p.Send(It.IsAny<GetPagesQuery>(), default)).Returns(Task.FromResult(pages));
            var service = CreateService();

            var result = await service.GetPagesAsync("996.icu", _username, _password, 996);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
        }

        [Test]
        public void GetPagesAsync_InvlaidNumPages()
        {
            var service = CreateService();
            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.GetPagesAsync("996.icu", _username, _password, -1);
            });
        }

        [Test]
        public async Task AddPageAsync_OK()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<CreatePageCommand>(), default))
                .Returns(Task.FromResult(Guid.Empty));
            var page = new Page
            {
                title = FakeData.FakePage.Title,
                description = "fubao"
            };
            var service = CreateService();

            var result = await service.AddPageAsync("996.icu", _username, _password, page, true);

            Assert.IsNotNull(result);
            Assert.AreEqual(Guid.Empty.ToString(), result);
            _mockMediator.Verify(p => p.Send(It.IsAny<CreatePageCommand>(), default));
        }

        [Test]
        public async Task EditPageAsync_OK()
        {
            _mockMediator
                .Setup(p => p.Send(It.IsAny<UpdatePageCommand>(), default))
                .Returns(Task.FromResult(Guid.Empty));
            var page = new Page
            {
                page_id = Guid.Empty.ToString(),
                title = FakeData.FakePage.Title,
                description = "fubao"
            };
            var service = CreateService();

            var result = await service.EditPageAsync("996.icu", Guid.Empty.ToString(), _username, _password, page, true);

            Assert.IsTrue(result);
            _mockMediator.Verify(p => p.Send(It.IsAny<UpdatePageCommand>(), default));
        }

        [Test]
        public void EditPageAsync_InvliadId()
        {
            var service = CreateService();

            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.EditPageAsync("996.icu", Guid.Empty.ToString(), _username, _password, null, true);
            });
        }

        [Test]
        public async Task DeletePostAsync_OK()
        {
            var service = CreateService();
            await service.DeletePostAsync("996.icu", FakeData.Uid1.ToString(), _username, _password, true);

            _mockPostManageService.Verify(p => p.DeleteAsync(It.IsAny<Guid>(), true));
        }

        [Test]
        public void DeletePostAsync_BadId()
        {
            var service = CreateService();

            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.DeletePostAsync("996.icu", "007", _username, _password, true);
            });
        }

        [Test]
        public async Task GetAuthorsAsync_OK()
        {
            var service = CreateService();
            var result = await service.GetAuthorsAsync("996.icu", _username, _password);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length == 1);
        }

        [Test]
        public async Task DeletePageAsync_OK()
        {
            var service = CreateService();
            var result = await service.DeletePageAsync("996.icu", _username, _password, FakeData.Uid2.ToString());

            Assert.IsTrue(result);
            _mockMediator.Verify(p => p.Send(It.IsAny<DeletePageCommand>(), default));
        }

        [Test]
        public void DeletePageAsync_BadId()
        {
            var service = CreateService();

            Assert.ThrowsAsync<MetaWeblogException>(async () =>
            {
                await service.DeletePageAsync("996.icu", _username, _password, "35");
            });
        }
    }
}
