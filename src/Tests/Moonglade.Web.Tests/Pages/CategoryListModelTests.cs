using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

    public class CategoryListModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IPostQueryService> _mockPostQueryService;

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
            var actionContext = new ActionContext(httpContext, new(), new PageActionDescriptor(), modelState);
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
            var result = await categoryListModel.OnGetAsync(FakeData.ShortString2);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGetAsync_ValidCat()
        {
            var cat = new Category
            {
                Id = Guid.Empty,
                DisplayName = FakeData.Title3,
                Note = "Get into ICU",
                RouteName = FakeData.Slug2
            };

            _mockCategoryService
                .Setup(p => p.Get(It.IsAny<string>()))
                .Returns(Task.FromResult(cat));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.PostCountCategory, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(35);

            _mockPostQueryService.Setup(p => p.List(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()))
                .Returns(Task.FromResult(FakeData.FakePosts));

            var categoryListModel = CreateCategoryListModel();
            var result = await categoryListModel.OnGetAsync(FakeData.Slug2);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(categoryListModel.Posts);
            Assert.AreEqual(35, categoryListModel.Posts.TotalItemCount);
        }
    }
}
