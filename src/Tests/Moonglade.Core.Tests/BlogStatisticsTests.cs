using Moonglade.Core.StatisticFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Core.Tests;

[TestFixture]
public class BlogStatisticsTests
{
    private MockRepository _mockRepository;
    private Mock<IRepository<PostExtensionEntity>> _mockPostExtensionRepo;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockPostExtensionRepo = _mockRepository.Create<IRepository<PostExtensionEntity>>();
    }

    [Test]
    public async Task GetStatisticAsync_StateUnderTest_ExpectedBehavior()
    {
        _mockPostExtensionRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(new PostExtensionEntity()
            {
                Hits = 996,
                Likes = 251
            }));

        // Arrange
        var handler = new GetStatisticQueryHandler(_mockPostExtensionRepo.Object);
        Guid postId = Guid.Empty;

        // Act
        var result = await handler.Handle(new(postId), default);

        // Assert
        Assert.AreEqual(996, result.Hits);
        Assert.AreEqual(251, result.Likes);
    }
}