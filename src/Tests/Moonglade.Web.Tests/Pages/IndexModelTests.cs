using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages;

[TestFixture]

public class IndexModelTests
{
    private MockRepository _mockRepository;

    private Mock<IBlogConfig> _mockBlogConfig;
    private Mock<IBlogCache> _mockBlogCache;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        _mockBlogCache = _mockRepository.Create<IBlogCache>();
        _mockMediator = _mockRepository.Create<IMediator>();

        _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
        {
            PostListPageSize = 10
        });
    }

    private IndexModel CreateIndexModel()
    {
        return new(
            _mockBlogConfig.Object,
            _mockBlogCache.Object,
            _mockMediator.Object);
    }

    [Test]
    public async Task OnGet_StateUnderTest_ExpectedBehavior()
    {
        _mockMediator.Setup(p => p.Send(It.IsAny<ListPostsQuery>(), default))
            .Returns(Task.FromResult(FakeData.FakePosts));

        _mockMediator.Setup(p => p.Send(It.IsAny<CountPostQuery>(), default)).Returns(Task.FromResult(FakeData.Int2));

        _mockBlogCache.Setup(p =>
                p.GetOrCreateAsync(CacheDivision.General, "postcount", It.IsAny<Func<ICacheEntry, Task<int>>>()))
            .Returns(Task.FromResult(FakeData.Int2));

        var indexModel = CreateIndexModel();
        int p = 1;

        await indexModel.OnGet(p);

        Assert.IsNotNull(indexModel.Posts);
        Assert.AreEqual(FakeData.Int2, indexModel.Posts.TotalItemCount);
    }
}