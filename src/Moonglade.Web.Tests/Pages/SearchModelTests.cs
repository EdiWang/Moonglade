using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;
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

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SearchModelTests
    {
        private MockRepository _mockRepository;
        private Mock<ISearchService> _mockSearchService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockSearchService = _mockRepository.Create<ISearchService>();
        }

        private SearchModel CreateSearchModel()
        {
            return new(_mockSearchService.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task OnGetAsync_EmptyTerm(string term)
        {
            var searchModel = CreateSearchModel();
            var result = await searchModel.OnGetAsync(term);

            Assert.IsInstanceOf<RedirectToPageResult>(result);
        }

        [Test]
        public async Task OnGetAsync_ValidTerm()
        {
            var fakePosts = new List<PostDigest>
            {
                new()
                {
                    Title = "Work 996 and get into ICU",
                    ContentAbstract = "This is Jack Ma's fubao",
                    LangCode = "en-us",
                    PubDateUtc = new(996, 9, 6),
                    Slug = "fuck-jack-ma",
                    Tags = new Tag[] {new()
                    {
                        DisplayName = "Fubao", NormalizedName = "fubao", Id = 996
                    }}
                }
            };

            _mockSearchService.Setup(p => p.SearchAsync(It.IsAny<string>()))
                .Returns(Task.FromResult((IReadOnlyList<PostDigest>)fakePosts));

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

            var searchModel = new SearchModel(_mockSearchService.Object)
            {
                PageContext = pageContext,
                TempData = tempData
            };

            var result = await searchModel.OnGetAsync("996");

            Assert.IsNotNull(searchModel.Posts);
        }
    }
}
