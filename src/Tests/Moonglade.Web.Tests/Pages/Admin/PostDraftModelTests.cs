using MediatR;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin;

[TestFixture]

public class PostDraftModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private PostDraftModel CreatePostDraftModel()
    {
        return new(_mockMediator.Object);
    }

    [Test]
    public async Task OnGet_StateUnderTest_ExpectedBehavior()
    {
        IReadOnlyList<PostSegment> data = new List<PostSegment>();

        _mockMediator.Setup(p => p.Send(It.IsAny<ListPostSegmentByStatusQuery>(), default)).Returns(Task.FromResult(data));

        var postDraftModel = CreatePostDraftModel();
        await postDraftModel.OnGet();

        Assert.IsNotNull(postDraftModel.PostSegments);
    }
}