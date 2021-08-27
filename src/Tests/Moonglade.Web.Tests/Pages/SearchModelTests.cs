using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]

    public class SearchModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private SearchModel CreateSearchModel()
        {
            return new(_mockMediator.Object);
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
                    Title = FakeData.Title2,
                    ContentAbstract = "This is Jack Ma's fubao",
                    LangCode = "en-us",
                    PubDateUtc = new(FakeData.Int2, 9, 6),
                    Slug = "fuck-jack-ma",
                    Tags = new Tag[] {new()
                    {
                        DisplayName = "Fubao", NormalizedName = FakeData.ShortString1, Id = FakeData.Int2
                    }}
                }
            };

            _mockMediator.Setup(p => p.Send(It.IsAny<SearchPostQuery>(), default))
                .Returns(Task.FromResult((IReadOnlyList<PostDigest>)fakePosts));

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

            var searchModel = new SearchModel(_mockMediator.Object)
            {
                PageContext = pageContext,
                TempData = tempData
            };

            var result = await searchModel.OnGetAsync(FakeData.ShortString2);

            Assert.IsNotNull(searchModel.Posts);
        }
    }
}
