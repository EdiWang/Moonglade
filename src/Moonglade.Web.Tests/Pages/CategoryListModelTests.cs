using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CategoryListModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IPostQueryService> _mockPostQueryService;

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

            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockPostQueryService = _mockRepository.Create<IPostQueryService>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        private CategoryListModel CreateCategoryListModel()
        {
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            var pageContext = new PageContext(actionContext)
            {
                ViewData = viewData
            };

            var model = new CategoryListModel(
                _mockCategoryService.Object,
                _mockBlogConfig.Object,
                _mockBlogCache.Object,
                _mockPostQueryService.Object)
            {
                PageContext = pageContext,
                TempData = tempData
            };

            return model;
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task OnGetAsync_EmptyRouteName(string routeName)
        {
            var categoryListModel = CreateCategoryListModel();
            var result = await categoryListModel.OnGetAsync(routeName);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGetAsync_NullCat()
        {
            _mockCategoryService
                .Setup(p => p.Get(It.IsAny<string>()))
                .Returns(Task.FromResult((Category)null));

            var categoryListModel = CreateCategoryListModel();
            var result = await categoryListModel.OnGetAsync("996");
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGetAsync_ValidCat()
        {
            var cat = new Category
            {
                Id = Guid.Empty,
                DisplayName = "Work 996",
                Note = "Get into ICU",
                RouteName = "work-996"
            };

            _mockCategoryService
                .Setup(p => p.Get(It.IsAny<string>()))
                .Returns(Task.FromResult(cat));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.PostCountCategory, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(35);

            _mockPostQueryService.Setup(p => p.List(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()))
                .Returns(Task.FromResult(_fakePosts));

            var categoryListModel = CreateCategoryListModel();
            var result = await categoryListModel.OnGetAsync("work-996");

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(categoryListModel.Posts);
            Assert.AreEqual(35, categoryListModel.Posts.TotalItemCount);
        }
    }
}
