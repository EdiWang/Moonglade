using MediatR;
using Moonglade.Core.TagFeature;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin;

[TestFixture]

public class TagsModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private TagsModel CreateTagsModel()
    {
        return new(_mockMediator.Object);
    }

    [Test]
    public async Task OnGet_StateUnderTest_ExpectedBehavior()
    {
        IReadOnlyList<Tag> tags = new List<Tag>
        {
            new() { Id = FakeData.Int2, DisplayName = FakeData.Title3, NormalizedName = FakeData.Slug2 }
        };
        _mockMediator.Setup(p => p.Send(It.IsAny<GetTagsQuery>(), default)).Returns(Task.FromResult(tags));

        var tagsModel = CreateTagsModel();
        await tagsModel.OnGet();

        Assert.IsNotNull(tagsModel.Tags);
    }
}