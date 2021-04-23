using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ArchiveListModelTests
    {
        private MockRepository _mockRepository;
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
            _mockPostQueryService = _mockRepository.Create<IPostQueryService>();
        }

        private ArchiveListModel CreateArchiveListModel()
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
            _mockPostQueryService.Setup(p => p.ListArchive(It.IsAny<int>(), null))
                .Returns(Task.FromResult(_fakePosts));

            var model = CreateArchiveListModel();
            var result = await model.OnGetAsync(2021, null);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.AreEqual(_fakePosts, model.Posts);
        }

        [Test]
        public async Task OnGetAsync_Year_Month()
        {
            _mockPostQueryService.Setup(p => p.ListArchive(It.IsAny<int>(), It.IsAny<int?>())).Returns(Task.FromResult(_fakePosts));

            var model = CreateArchiveListModel();
            var result = await model.OnGetAsync(2021, 1);
            Assert.IsInstanceOf<PageResult>(result);
            Assert.AreEqual(_fakePosts, model.Posts);
        }
    }
}
