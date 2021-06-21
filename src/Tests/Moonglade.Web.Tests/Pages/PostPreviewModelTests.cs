using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]

    public class PostPreviewModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostQueryService> _mockPostQueryService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostQueryService = _mockRepository.Create<IPostQueryService>();
        }

        private PostPreviewModel CreatePostPreviewModel()
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

            var model = new PostPreviewModel(_mockPostQueryService.Object)
            {
                PageContext = pageContext,
                TempData = tempData
            };

            return model;
        }

        [Test]
        public async Task OnGetAsync_NotFound()
        {
            _mockPostQueryService.Setup(p => p.GetDraftAsync(Guid.Empty))
                .Returns(Task.FromResult((Post)null));
            var postPreviewModel = CreatePostPreviewModel();

            var result = await postPreviewModel.OnGetAsync(Guid.Empty);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGetAsync_Found()
        {
            _mockPostQueryService.Setup(p => p.GetDraftAsync(Guid.Empty))
                .Returns(Task.FromResult(new Post()));
            var postPreviewModel = CreatePostPreviewModel();

            var result = await postPreviewModel.OnGetAsync(Guid.Empty);

            Assert.IsNotNull(postPreviewModel.Post);
            Assert.IsInstanceOf<PageResult>(result);
        }
    }
}
