using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin;

[TestFixture]
public class EditPageModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private EditPageModel CreateEditPageModel()
    {
        return new(_mockMediator.Object);
    }

    [Test]
    public async Task OnGetAsync_NoId()
    {
        var editPageModel = CreateEditPageModel();
        var result = await editPageModel.OnGetAsync(null);

        Assert.IsInstanceOf<PageResult>(result);
    }

    [Test]
    public async Task OnGetAsync_NoPage()
    {
        _mockMediator.Setup(p => p.Send(It.IsAny<GetPageByIdQuery>(), default))
            .Returns(Task.FromResult((BlogPage)null));

        var editPageModel = CreateEditPageModel();
        var result = await editPageModel.OnGetAsync(Guid.Empty);

        Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [Test]
    public async Task OnGetAsync_HasPage()
    {
        var fakePage = new BlogPage
        {
            Id = Guid.Empty,
            CreateTimeUtc = new(FakeData.Int2, 9, 6),
            CssContent = ".jack-ma .heart {color: black !important;}",
            HideSidebar = false,
            IsPublished = false,
            MetaDescription = "Fuck Jack Ma",
            RawHtmlContent = "<p>Fuck 996</p>",
            Slug = "fuck-jack-ma",
            Title = "Fuck Jack Ma 1000 years!",
            UpdateTimeUtc = new DateTime(1996, 9, 6)
        };
        _mockMediator.Setup(p => p.Send(It.IsAny<GetPageByIdQuery>(), default))
            .Returns(Task.FromResult(fakePage));

        var editPageModel = CreateEditPageModel();
        var result = await editPageModel.OnGetAsync(Guid.Empty);

        Assert.IsInstanceOf<PageResult>(result);
        Assert.IsNotNull(editPageModel.EditPageRequest);
    }
}