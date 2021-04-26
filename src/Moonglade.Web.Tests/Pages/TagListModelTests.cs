using System;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class TagListModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ITagService> _mockTagService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IPostQueryService> _mockPostQueryService;
        private Mock<IBlogCache> _mockBlogCache;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockTagService = _mockRepository.Create<ITagService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockPostQueryService = _mockRepository.Create<IPostQueryService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        private TagListModel CreateTagListModel()
        {
            return new(
                _mockTagService.Object,
                _mockBlogConfig.Object,
                _mockPostQueryService.Object,
                _mockBlogCache.Object);
        }

        [Test]
        public async Task OnGet_NullTag()
        {
            _mockTagService.Setup(p => p.Get(It.IsAny<string>())).Returns((Tag)null);

            // Arrange
            var tagListModel = CreateTagListModel();
            string normalizedName = FakeData.ShortString2;

            // Act
            var result = await tagListModel.OnGet(normalizedName);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGet_ValidTag()
        {
            _mockTagService.Setup(p => p.Get(It.IsAny<string>())).Returns(new Tag
            {
                Id = 996,
                DisplayName = "Fubao",
                NormalizedName = "fu-bao"
            });

            _mockPostQueryService.Setup(p => p.ListByTag(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(FakeData.FakePosts));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.PostCountTag, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(FakeData.Int1);

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

            var model = new TagListModel(
                _mockTagService.Object,
                _mockBlogConfig.Object,
                _mockPostQueryService.Object,
                _mockBlogCache.Object)
            {
                PageContext = pageContext,
                TempData = tempData
            };

            string normalizedName = "fu-bao";

            // Act
            var result = await model.OnGet(normalizedName);
            Assert.IsInstanceOf<PageResult>(result);
            Assert.AreEqual(FakeData.Int1, model.Posts.TotalItemCount);
        }
    }
}
