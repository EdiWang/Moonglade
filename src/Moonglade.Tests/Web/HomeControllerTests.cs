using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using X.PagedList;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class HomeControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IPostService> _mockPostService;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ILogger<HomeController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);

            _mockPostService = _mockRepository.Create<IPostService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockLogger = _mockRepository.Create<ILogger<HomeController>>();
        }

        private HomeController CreateHomeController()
        {
            return new(
                _mockPostService.Object,
                _mockBlogCache.Object,
                _mockBlogConfig.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task Index_View()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings()
            {
                PostListPageSize = 10
            });

            var fakePosts = new List<PostListEntry>
            {
                new()
                {
                    Title = "“996”工作制，即每天早 9 点到岗，一直工作到晚上 9 点，每周工作 6 天。",
                    ContentAbstract = "中国大陆工时规管现况（标准工时）： 一天工作时间为 8 小时，平均每周工时不超过 40 小时；加班上限为一天 3 小时及一个月 36 小时，逾时工作薪金不低于平日工资的 150%。而一周最高工时则为 48 小时。平均每月计薪天数为 21.75 天。",
                    LangCode = "zh-CN",
                    PubDateUtc = new (996,9,6),
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

            _mockPostService.Setup(p => p.GetPagedPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()))
                .Returns(Task.FromResult((IReadOnlyList<PostListEntry>)fakePosts));

            _mockPostService.Setup(p => p.CountVisiblePosts()).Returns(996);

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.General, "postcount", It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(996);

            var ctl = CreateHomeController();
            var result = await ctl.Index();

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<StaticPagedList<PostListEntry>>(model);

            var pagedList = (StaticPagedList<PostListEntry>)model;
            Assert.AreEqual(996, pagedList.TotalItemCount);
        }

        [Test]
        public async Task Tags_Index()
        {
            var fakeTags = new List<DegreeTag>
            {
                new() { Degree = 251, DisplayName = "Huawei", Id = 35, NormalizedName = "aiguo" },
                new() { Degree = 996, DisplayName = "Ali", Id = 35, NormalizedName = "fubao" }
            };

            var mockTagService = new Mock<ITagService>();
            mockTagService.Setup(p => p.GetTagCountListAsync())
                .Returns(Task.FromResult((IReadOnlyList<DegreeTag>)fakeTags));

            var ctl = CreateHomeController();
            var result = await ctl.Tags(mockTagService.Object);

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

            var mockArchiveService = new Mock<IPostArchiveService>();
            mockArchiveService.Setup(p => p.ListAsync())
                .Returns(Task.FromResult((IReadOnlyList<Archive>)fakeArchives));

            var ctl = CreateHomeController();
            var result = await ctl.Archive(mockArchiveService.Object);

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(fakeArchives, ((ViewResult)result).Model);
        }

        [Test]
        public async Task ArchiveList_BadYear()
        {
            var mockArchiveService = new Mock<IPostArchiveService>();

            var ctl = CreateHomeController();
            var result = await ctl.ArchiveList(mockArchiveService.Object, 9999, 1);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task ArchiveList_Year()
        {
            var fakePosts = new List<PostListEntry>
            {
                new()
                {
                    Title = "什么是 996.ICU？工作 996，生病 ICU。",
                    ContentAbstract = "2016 年 9 月初起，陆续有网友爆料称，58同城实行全员 996 工作制，且周末加班没有工资。公司方面回应称，为应对业务量高峰期，公司每年 9、10 月份都会有动员，属常规性活动，而本次“996 动员”并非强制。（58同城实行全员996工作制 被指意图逼员工主动辞职. 央广网. 2016-09-01. ）",
                    LangCode = "zh-CN",
                    PubDateUtc = new (2021,9,6),
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

            var mockArchiveService = new Mock<IPostArchiveService>();
            mockArchiveService.Setup(p => p.ListPostsAsync(It.IsAny<int>(), 0))
                .Returns(Task.FromResult((IReadOnlyList<PostListEntry>)fakePosts));

            var ctl = CreateHomeController();
            var result = await ctl.ArchiveList(mockArchiveService.Object, 2021, null);

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.AreEqual(fakePosts, (IReadOnlyList<PostListEntry>)model);

            Assert.AreEqual("2021", ((ViewResult)result).ViewData["ArchiveInfo"]);
        }
    }
}
