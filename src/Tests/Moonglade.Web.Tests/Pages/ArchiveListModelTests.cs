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

    public class ArchiveListModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostQueryService> _mockPostQueryService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostQueryService = _mockRepository.Create<IPostQueryService>();
        }

        private ArchiveListModel CreateArchiveListModel()
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

            var model = new ArchiveListModel(_mockPostQueryService.Object)
            {
                PageContext = pageContext,
                TempData = tempData
            };

            return model;
        }

        [Test]
        public async Task OnGetAsync_BadYear()
        {
            // Arrange
            var archiveListModel = CreateArchiveListModel();
            int year = 9999;
            int? month = 1;

            var result = await archiveListModel.OnGetAsync(
                year,
                month);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task OnGetAsync_Year()
        {
            _mockPostQueryService.Setup(p => p.ListArchiveAsync(It.IsAny<int>(), null))
                .Returns(Task.FromResult(FakeData.FakePosts));

            var model = CreateArchiveListModel();
            var result = await model.OnGetAsync(2021, null);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(model.Posts);
        }

        [Test]
        public async Task OnGetAsync_Year_Month()
        {
            _mockPostQueryService.Setup(p => p.ListArchiveAsync(It.IsAny<int>(), It.IsAny<int?>())).Returns(Task.FromResult(FakeData.FakePosts));

            var model = CreateArchiveListModel();
            var result = await model.OnGetAsync(2021, 1);
            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(model.Posts);
        }
    }
}
