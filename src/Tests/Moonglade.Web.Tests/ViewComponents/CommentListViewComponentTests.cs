using MediatR;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;
using Moonglade.Comments;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.ViewComponents;

[TestFixture]
public class CommentListViewComponentTests
{
    private MockRepository _mockRepository;

    private Mock<ILogger<CommentListViewComponent>> _mockLogger;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockLogger = _mockRepository.Create<ILogger<CommentListViewComponent>>();
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private CommentListViewComponent CreateComponent()
    {
        return new(
            _mockLogger.Object,
            _mockMediator.Object);
    }

    [Test]
    public async Task InvokeAsync_Exception()
    {
        _mockMediator.Setup(p => p.Send(It.IsAny<GetApprovedCommentsQuery>(), default)).Throws(new(FakeData.ShortString2));

        var component = CreateComponent();
        var result = await component.InvokeAsync(Guid.Parse("5ef8cb0d-963d-47e9-802c-48e40c7f4ef5"));

        Assert.IsInstanceOf<ContentViewComponentResult>(result);
    }

    [Test]
    public async Task InvokeAsync_EmptyId()
    {
        var component = CreateComponent();
        var result = await component.InvokeAsync(Guid.Empty);

        Assert.IsInstanceOf<ContentViewComponentResult>(result);
    }

    [Test]
    public async Task InvokeAsync_View()
    {
        IReadOnlyList<Comment> comments = new List<Comment>();

        _mockMediator.Setup(p => p.Send(It.IsAny<GetApprovedCommentsQuery>(), default)).Returns(Task.FromResult(comments));

        var component = CreateComponent();
        var result = await component.InvokeAsync(Guid.Parse("5ef8cb0d-963d-47e9-802c-48e40c7f4ef5"));

        Assert.IsInstanceOf<ViewViewComponentResult>(result);
        Assert.AreEqual(null, ((ViewViewComponentResult)result).ViewName);
    }
}