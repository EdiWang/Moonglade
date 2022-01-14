using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages;

[TestFixture]
public class PagePreviewModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    readonly BlogPage _fakeBlogPage = new()
    {
        Id = Guid.Empty,
        CreateTimeUtc = new(996, 9, 6),
        CssContent = ".jack-ma .heart {color: black !important;}",
        HideSidebar = false,
        IsPublished = false,
        MetaDescription = "Fuck Jack Ma",
        RawHtmlContent = "<p>Fuck 996</p>",
        Slug = "fuck-jack-ma",
        Title = "Fuck Jack Ma 1000 years!",
        UpdateTimeUtc = new(1996, 9, 6)
    };

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private PagePreviewModel CreatePagePreviewModel()
    {
        return new(_mockMediator.Object);
    }

    [Test]
    public async Task OnGetAsync_NoPage()
    {
        _mockMediator.Setup(p => p.Send(It.IsAny<GetPageByIdQuery>(), default))
            .Returns(Task.FromResult((BlogPage)null));

        var blogPagePreviewModel = CreatePagePreviewModel();
        var result = await blogPagePreviewModel.OnGetAsync(Guid.Empty);

        Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [Test]
    public async Task OnGetAsync_HasPage()
    {
        _mockMediator.Setup(p => p.Send(It.IsAny<GetPageByIdQuery>(), default))
            .Returns(Task.FromResult(_fakeBlogPage));

        var pagePreviewModel = CreatePagePreviewModel();
        var result = await pagePreviewModel.OnGetAsync(Guid.Empty);

        Assert.IsInstanceOf<PageResult>(result);
        Assert.IsNotNull(pagePreviewModel.BlogPage);
        Assert.AreEqual(_fakeBlogPage.Title, pagePreviewModel.BlogPage.Title);
    }
}