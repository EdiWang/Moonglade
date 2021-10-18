using MediatR;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin;

[TestFixture]

public class PostInsightsModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private PostInsightsModel CreatePostInsightsModel()
    {
        return new(_mockMediator.Object);
    }

    [Test]
    public async Task OnGet_StateUnderTest_ExpectedBehavior()
    {
        IReadOnlyList<PostSegment> segments = new List<PostSegment>();

        _mockMediator.Setup(p => p.Send(It.IsAny<ListInsightsQuery>(), default))
            .Returns(Task.FromResult(segments));

        var postInsightsModel = CreatePostInsightsModel();
        await postInsightsModel.OnGet();

        Assert.IsNotNull(postInsightsModel.TopReadList);
        Assert.IsNotNull(postInsightsModel.TopCommentedList);
    }
}