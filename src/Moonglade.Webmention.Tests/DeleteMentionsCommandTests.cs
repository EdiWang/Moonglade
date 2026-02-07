using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moq;

namespace Moonglade.Webmention.Tests;

public class DeleteMentionsCommandTests
{
    private readonly Mock<IRepositoryBase<MentionEntity>> _mockRepo;
    private readonly DeleteMentionsCommandHandler _handler;

    public DeleteMentionsCommandTests()
    {
        _mockRepo = new Mock<IRepositoryBase<MentionEntity>>();
        _handler = new DeleteMentionsCommandHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_NullIds_ReturnsWithoutDeleting()
    {
        var command = new DeleteMentionsCommand(null!);

        await _handler.HandleAsync(command, CancellationToken.None);

        _mockRepo.Verify(r => r.ListAsync(It.IsAny<MentionByIdsSpec>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<MentionEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmptyIds_ReturnsWithoutDeleting()
    {
        var command = new DeleteMentionsCommand([]);

        await _handler.HandleAsync(command, CancellationToken.None);

        _mockRepo.Verify(r => r.ListAsync(It.IsAny<MentionByIdsSpec>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<MentionEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoMatchingEntities_DoesNotDelete()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var command = new DeleteMentionsCommand(ids);

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<MentionByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MentionEntity>());

        await _handler.HandleAsync(command, CancellationToken.None);

        _mockRepo.Verify(r => r.ListAsync(It.IsAny<MentionByIdsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<MentionEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingEntities_DeletesThem()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var ids = new List<Guid> { id1, id2 };
        var command = new DeleteMentionsCommand(ids);

        var entities = new List<MentionEntity>
        {
            new() { Id = id1 },
            new() { Id = id2 }
        };

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<MentionByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        await _handler.HandleAsync(command, CancellationToken.None);

        _mockRepo.Verify(r => r.ListAsync(It.IsAny<MentionByIdsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.DeleteRangeAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
    }
}
