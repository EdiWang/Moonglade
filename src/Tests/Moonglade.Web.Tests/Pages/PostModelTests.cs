using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages;

[TestFixture]

public class PostModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private PostModel CreatePostModel()
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

        var model = new PostModel(_mockMediator.Object)
        {
            PageContext = pageContext,
            TempData = tempData
        };

        return model;
    }

    [Test]
    public async Task OnGetAsync_YearOutOfRange()
    {
        var postModel = CreatePostModel();
        var result = await postModel.OnGetAsync(DateTime.UtcNow.Year + 1, 9, 9, "6");

        Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task OnGetAsync_EmptySlug(string slug)
    {
        var postModel = CreatePostModel();
        var result = await postModel.OnGetAsync(DateTime.UtcNow.Year + 1, 9, 9, slug);

        Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [Test]
    public async Task OnGetAsync_NullPost()
    {
        _mockMediator.Setup(p => p.Send(It.IsAny<GetPostBySlugQuery>(), default))
            .Returns(Task.FromResult((Post)null));

        var postModel = CreatePostModel();
        var result = await postModel.OnGetAsync(DateTime.UtcNow.Year, 1, 9, FakeData.Slug2);

        Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [Test]
    public async Task Slug_View()
    {
        _mockMediator.Setup(p => p.Send(It.IsAny<GetPostBySlugQuery>(), default))
            .Returns(Task.FromResult(new Post
            {
                Id = Guid.Empty,
                Slug = FakeData.Slug2,
                Title = FakeData.Title3
            }));

        var postModel = CreatePostModel();
        var result = await postModel.OnGetAsync(DateTime.UtcNow.Year, 1, 9, FakeData.Slug2);

        Assert.IsInstanceOf<PageResult>(result);
    }
}