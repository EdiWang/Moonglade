using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Pages;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using X.PagedList;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class HomeControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IPostService> _mockPostService;
        private Mock<IPageService> _mockPageService;
        private Mock<ITagService> _mockTagService;
        private Mock<IBlogArchiveService> _mockArchiveService;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ILogger<HomeController>> _mockLogger;
        private Mock<IOptions<AppSettings>> _mockAppSettingsOptions;


        private readonly IReadOnlyList<PostDigest> _fakePosts = new List<PostDigest>
        {
            new()
            {
                Title = "“996”工作制，即每天早 9 点到岗，一直工作到晚上 9 点，每周工作 6 天。",
                ContentAbstract = "中国大陆工时规管现况（标准工时）： 一天工作时间为 8 小时，平均每周工时不超过 40 小时；加班上限为一天 3 小时及一个月 36 小时，逾时工作薪金不低于平日工资的 150%。而一周最高工时则为 48 小时。平均每月计薪天数为 21.75 天。",
                LangCode = "zh-CN",
                PubDateUtc = new(996, 9, 6),
                Slug = "996-icu",
                Tags = new Tag[]{
                    new ()
                    {
                        DisplayName = "996",
                        Id = 996,
                        NormalizedName = "icu"
                    }
                }
            }
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockPostService = _mockRepository.Create<IPostService>();
            _mockPageService = _mockRepository.Create<IPageService>();
            _mockTagService = _mockRepository.Create<ITagService>();
            _mockArchiveService = _mockRepository.Create<IBlogArchiveService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockLogger = _mockRepository.Create<ILogger<HomeController>>();
            _mockAppSettingsOptions = _mockRepository.Create<IOptions<AppSettings>>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        private HomeController CreateHomeController()
        {
            return new(
                _mockPostService.Object,
                _mockPageService.Object,
                _mockTagService.Object,
                _mockArchiveService.Object,
                _mockBlogCache.Object,
                _mockBlogConfig.Object,
                _mockLogger.Object,
                _mockAppSettingsOptions.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task Page_EmptySlug(string slug)
        {
            var ctl = CreateHomeController();
            var result = await ctl.Page(slug);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task Page_NotFound_Null()
        {
            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.Page, "996", It.IsAny<Func<ICacheEntry, BlogPage>>()))
                .Returns((BlogPage)null);

            var ctl = CreateHomeController();
            var result = await ctl.Page("996");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Page_NotFound_Unpublished()
        {
            var page = new BlogPage { IsPublished = false };

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.Page, "996", It.IsAny<Func<ICacheEntry, BlogPage>>()))
                .Returns(page);

            var ctl = CreateHomeController();
            var result = await ctl.Page("996");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Index_View()
        {
            _mockPostService.Setup(p => p.List(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()))
                .Returns(Task.FromResult(_fakePosts));

            _mockPostService.Setup(p => p.CountPublic()).Returns(996);

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.General, "postcount", It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(996);

            var ctl = CreateHomeController();
            var result = await ctl.Index();

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<StaticPagedList<PostDigest>>(model);

            var pagedList = (StaticPagedList<PostDigest>)model;
            Assert.AreEqual(996, pagedList.TotalItemCount);
        }

        [Test]
        public async Task Tags_Index()
        {
            var fakeTags = new List<KeyValuePair<Tag, int>>
            {
                new(new Tag { DisplayName = "Huawei", Id = 35, NormalizedName = "aiguo" }, 251),
                new(new Tag { DisplayName = "Ali", Id = 35, NormalizedName = "fubao" }, 996)
            };

            _mockTagService.Setup(p => p.GetTagCountList())
                .Returns(Task.FromResult((IReadOnlyList<KeyValuePair<Tag, int>>)fakeTags));

            var ctl = CreateHomeController();
            var result = await ctl.Tags();

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(fakeTags, ((ViewResult)result).Model);
        }

        [Test]
        public async Task Archive_Index()
        {
            var fakeArchives = new List<Archive>
            {
                new (996,9,6),
                new (251,3,5)
            };

            _mockArchiveService.Setup(p => p.ListAsync())
                .Returns(Task.FromResult((IReadOnlyList<Archive>)fakeArchives));

            var ctl = CreateHomeController();
            var result = await ctl.Archive();

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(fakeArchives, ((ViewResult)result).Model);
        }

        [Test]
        public async Task TagList_NullTag()
        {
            _mockTagService.Setup(p => p.Get(It.IsAny<string>())).Returns((Tag)null);

            var ctl = CreateHomeController();
            var result = await ctl.TagList("996", 1);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task TagList_ValidTag()
        {
            _mockTagService.Setup(p => p.Get(It.IsAny<string>())).Returns(new Tag
            {
                Id = 996,
                DisplayName = "Fubao",
                NormalizedName = "fu-bao"
            });

            _mockPostService.Setup(p => p.ListByTag(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(_fakePosts));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.PostCountTag, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(251);

            var ctl = CreateHomeController();
            var result = await ctl.TagList("fu-bao");

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<StaticPagedList<PostDigest>>(model);

            var pagedList = (StaticPagedList<PostDigest>)model;
            Assert.AreEqual(251, pagedList.TotalItemCount);
        }

        [Test]
        public async Task Featured_View()
        {
            _mockPostService.Setup(p => p.ListFeatured(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(_fakePosts));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.PostCountFeatured, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(251);

            var ctl = CreateHomeController();
            var result = await ctl.Featured();

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<StaticPagedList<PostDigest>>(model);

            var pagedList = (StaticPagedList<PostDigest>)model;
            Assert.AreEqual(251, pagedList.TotalItemCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task CategoryList_EmptyRouteName(string routeName)
        {
            var mockCatService = new Mock<ICategoryService>();

            var ctl = CreateHomeController();
            var result = await ctl.CategoryList(mockCatService.Object, routeName);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task CategoryList_NullCat()
        {
            var mockCatService = new Mock<ICategoryService>();
            mockCatService
                .Setup(p => p.Get(It.IsAny<string>()))
                .Returns(Task.FromResult((Category)null));

            var ctl = CreateHomeController();
            var result = await ctl.CategoryList(mockCatService.Object, "996");

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task CategoryList_ValidCat()
        {
            var cat = new Category
            {
                Id = Guid.Empty,
                DisplayName = "Work 996",
                Note = "Get into ICU",
                RouteName = "work-996"
            };

            var mockCatService = new Mock<ICategoryService>();
            mockCatService
                .Setup(p => p.Get(It.IsAny<string>()))
                .Returns(Task.FromResult(cat));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.PostCountCategory, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(35);

            _mockPostService.Setup(p => p.List(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()))
                .Returns(Task.FromResult(_fakePosts));

            var ctl = CreateHomeController();
            var result = await ctl.CategoryList(mockCatService.Object, "work-996");

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<StaticPagedList<PostDigest>>(model);

            var pagedList = (StaticPagedList<PostDigest>)model;
            Assert.AreEqual(35, pagedList.TotalItemCount);

            Assert.AreEqual(cat.DisplayName, ((ViewResult)result).ViewData["CategoryDisplayName"]);
            Assert.AreEqual(cat.RouteName, ((ViewResult)result).ViewData["CategoryRouteName"]);
            Assert.AreEqual(cat.Note, ((ViewResult)result).ViewData["CategoryDescription"]);
        }

        [Test]
        public async Task ArchiveList_BadYear()
        {
            var ctl = CreateHomeController();
            var result = await ctl.ArchiveList(9999, 1);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task ArchiveList_Year()
        {
            _mockArchiveService.Setup(p => p.ListPostsAsync(It.IsAny<int>(), 0))
                .Returns(Task.FromResult(_fakePosts));

            var ctl = CreateHomeController();
            var result = await ctl.ArchiveList(2021, null);

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.AreEqual(_fakePosts, (IReadOnlyList<PostDigest>)model);

            Assert.AreEqual("2021", ((ViewResult)result).ViewData["ArchiveInfo"]);
        }

        [Test]
        public async Task ArchiveList_Year_Month()
        {
            _mockArchiveService.Setup(p => p.ListPostsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(_fakePosts));

            var ctl = CreateHomeController();
            var result = await ctl.ArchiveList(2021, 1);

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.AreEqual(_fakePosts, (IReadOnlyList<PostDigest>)model);

            Assert.AreEqual("2021.1", ((ViewResult)result).ViewData["ArchiveInfo"]);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetLanguage_EmptyCulture(string culture)
        {
            var ctl = CreateHomeController();
            var result = ctl.SetLanguage(culture, null);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public void SetLanguage_Cookie()
        {
            var ctl = CreateHomeController();
            var result = ctl.SetLanguage("en-US", "/996/icu");

            Assert.IsInstanceOf<LocalRedirectResult>(result);
        }
    }
}
