using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.FriendLink;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Core.Tests;

[TestFixture]
public class FriendLinkTests
{
    private MockRepository _mockRepository;
    private Mock<IRepository<FriendLinkEntity>> _mockFriendlinkRepo;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockFriendlinkRepo = _mockRepository.Create<IRepository<FriendLinkEntity>>();
    }

    [Test]
    public async Task GetAsync_OK()
    {
        var handler = new GetLinkQueryHandler(_mockFriendlinkRepo.Object);
        await handler.Handle(new(Guid.Empty), CancellationToken.None);

        _mockFriendlinkRepo.Verify(p =>
            p.SelectFirstOrDefaultAsync(It.IsAny<ISpecification<FriendLinkEntity>>(),
                It.IsAny<Expression<Func<FriendLinkEntity, Link>>>()));
    }

    [Test]
    public async Task GetAllAsync_OK()
    {
        var handler = new GetAllLinksQueryHandler(_mockFriendlinkRepo.Object);
        await handler.Handle(new(), CancellationToken.None);

        _mockFriendlinkRepo.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<FriendLinkEntity, Link>>>()));
    }

    [Test]
    public void AddAsync_InvalidUrl()
    {
        var handler = new AddLinkCommandHandler(_mockFriendlinkRepo.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(new()
            {
                LinkUrl = "Fubao",
                Title = "work006"
            }), CancellationToken.None);
        });
    }

    [Test]
    public async Task AddAsync_Valid()
    {
        var uid = Guid.NewGuid();
        var friendLinkEntity = new FriendLinkEntity
        {
            Id = uid,
            LinkUrl = "https://dot.net",
            Title = "Choice of 955"
        };
        var tcs = new TaskCompletionSource<FriendLinkEntity>();
        tcs.SetResult(friendLinkEntity);

        _mockFriendlinkRepo.Setup(p => p.AddAsync(It.IsAny<FriendLinkEntity>())).Returns(tcs.Task);

        var handler = new AddLinkCommandHandler(_mockFriendlinkRepo.Object);
        await handler.Handle(new(new()
        {
            LinkUrl = "https://dot.net",
            Title = "Choice of 955"
        }), CancellationToken.None);

        Assert.Pass();
    }

    [Test]
    public async Task DeleteAsync_OK()
    {
        var handler = new DeleteLinkCommandHandler(_mockFriendlinkRepo.Object);
        await handler.Handle(new(Guid.Empty), CancellationToken.None);

        Assert.Pass();
    }

    [Test]
    public void UpdateAsync_InvalidUrl()
    {
        var handler = new UpdateLinkCommandHandler(_mockFriendlinkRepo.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Guid.Empty, new()
            {
                LinkUrl = "Fubao",
                Title = "work006"
            }), default);
        });
    }

    [Test]
    public async Task UpdateAsync_LinkNull()
    {
        _mockFriendlinkRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()));

        var handler = new UpdateLinkCommandHandler(_mockFriendlinkRepo.Object);
        await handler.Handle(new(Guid.Empty, new()
        {
            LinkUrl = "https://996.icu",
            Title = "work"
        }), default);

        _mockFriendlinkRepo.Verify(p => p.UpdateAsync(It.IsAny<FriendLinkEntity>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_OK()
    {
        _mockFriendlinkRepo.Setup(p => p.GetAsync(It.IsAny<Guid>())).Returns(ValueTask.FromResult(new FriendLinkEntity
        {
            Id = Guid.Empty,
            LinkUrl = "https://dot.net",
            Title = "Choice of 955"
        }));

        var handler = new UpdateLinkCommandHandler(_mockFriendlinkRepo.Object);
        await handler.Handle(new(Guid.Empty, new()
        {
            LinkUrl = "https://996.icu",
            Title = "work"
        }), default);

        _mockFriendlinkRepo.Verify(p => p.UpdateAsync(It.IsAny<FriendLinkEntity>()));
    }
}