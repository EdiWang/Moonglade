using Moonglade.Comments.Moderators;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Comments.Tests;

[TestFixture]
public class CommentHandlersTests
{
    private MockRepository _mockRepository;

    private Mock<IBlogConfig> _mockBlogConfig;
    private Mock<IRepository<CommentEntity>> _mockCommentEntityRepo;
    private Mock<IRepository<CommentReplyEntity>> _mockCommentReplyEntityRepo;
    private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
    private Mock<ICommentModerator> _mockCommentModerator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        _mockCommentEntityRepo = _mockRepository.Create<IRepository<CommentEntity>>();
        _mockCommentReplyEntityRepo = _mockRepository.Create<IRepository<CommentReplyEntity>>();
        _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
        _mockCommentModerator = _mockRepository.Create<ICommentModerator>();
    }

    [Test]
    public async Task Count_ExpectedBehavior()
    {
        _mockCommentEntityRepo.Setup(p => p.Count(t => true)).Returns(996);
        var handler = new CountCommentsQueryHandler(_mockCommentEntityRepo.Object);
        var result = await handler.Handle(new(), default);
        Assert.AreEqual(996, result);
    }

    [Test]
    public async Task GetApprovedCommentsAsync_OK()
    {
        var handler = new GetApprovedCommentsQueryHandler(_mockCommentEntityRepo.Object);
        await handler.Handle(new(Guid.Empty), default);

        _mockCommentEntityRepo.Verify(p => p.SelectAsync(It.IsAny<ISpecification<CommentEntity>>(),
            It.IsAny<Expression<Func<CommentEntity, Comment>>>()));
    }

    [Test]
    public void ToggleApprovalAsync_EmptyIds()
    {
        var handler = new ToggleApprovalCommandHandler(_mockCommentEntityRepo.Object);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.Handle(new(Array.Empty<Guid>()), default);
        });
    }

    [Test]
    public async Task ToggleApprovalAsync_OK()
    {
        CommentEntity cmt = new()
        {
            Id = Guid.Empty,
            CreateTimeUtc = new(996, 9, 9, 6, 3, 5),
            CommentContent = "work 996 and get into icu",
            Email = "fubao@996.icu",
            IPAddress = "251.251.251.251",
            IsApproved = false,
            Username = "Jack Ma"
        };

        IReadOnlyList<CommentEntity> fakeComments = new List<CommentEntity>
        {
            cmt
        };

        _mockCommentEntityRepo.Setup(p => p.GetAsync(It.IsAny<CommentSpec>())).Returns(Task.FromResult(fakeComments));

        var handler = new ToggleApprovalCommandHandler(_mockCommentEntityRepo.Object);
        await handler.Handle(new(new[] { Guid.Empty }), default);

        Assert.IsTrue(cmt.IsApproved);
        _mockCommentEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<CommentEntity>()));
    }

    [Test]
    public void DeleteAsync_EmptyIds()
    {
        var handler = new DeleteCommentsCommandHandler(_mockCommentEntityRepo.Object,
            _mockCommentReplyEntityRepo.Object);

        Assert.ThrowsAsync<ArgumentNullException>(async () => { await handler.Handle(new(Array.Empty<Guid>()), default); });
    }

    [Test]
    public async Task DeleteAsync_OK()
    {
        IReadOnlyList<CommentEntity> comments = new List<CommentEntity>
        {
            new()
            {
                Id = Guid.Empty,
                CommentContent = "Work 996",
                CreateTimeUtc = DateTime.MinValue,
                Email = "worker@996.icu",
                IPAddress = "9.9.6.35",
                IsApproved = true,
                Username = "996 Worker"
            }
        };
        _mockCommentEntityRepo
            .Setup(p => p.GetAsync(It.IsAny<CommentSpec>()))
            .Returns(Task.FromResult(comments));

        IReadOnlyList<CommentReplyEntity> replyEntities = new List<CommentReplyEntity>()
        {
            new()
            {
                Id = Guid.Parse("29979c8b-9184-4422-94a8-e35022f9c8c5"),
                CommentId = Guid.Empty,
                ReplyContent = "And wear green hat",
                CreateTimeUtc = DateTime.MinValue.AddDays(1)
            }
        };
        _mockCommentReplyEntityRepo
            .Setup(p => p.GetAsync(It.IsAny<CommentReplySpec>()))
            .Returns(Task.FromResult(replyEntities));

        var handler = new DeleteCommentsCommandHandler(_mockCommentEntityRepo.Object,
            _mockCommentReplyEntityRepo.Object);

        await handler.Handle(new(new[] { Guid.Empty }), default);

        _mockCommentReplyEntityRepo.Verify(p => p.DeleteAsync(It.IsAny<IEnumerable<CommentReplyEntity>>()));
        _mockCommentEntityRepo.Verify(p => p.DeleteAsync(It.IsAny<CommentEntity>()));
    }

    [Test]
    public async Task CreateAsync_OK()
    {
        _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
        {
            EnableWordFilter = true,
            WordFilterMode = WordFilterMode.Mask,
            RequireCommentReview = true
        });

        _mockPostEntityRepo
            .Setup(p => p.SelectFirstOrDefaultAsync(It.IsAny<PostSpec>(), p => p.Title))
            .Returns(Task.FromResult("996 is Fubao"));

        CommentRequest req = new CommentRequest
        {
            Content = "Work 996 and get into ICU",
            Email = "worker@996.icu",
            Username = "Fubao Collector"
        };
        var handler = new CreateCommentCommandHandler(_mockBlogConfig.Object, _mockPostEntityRepo.Object,
            _mockCommentModerator.Object, _mockCommentEntityRepo.Object);

        var result = await handler.Handle(new(Guid.Empty, req, "251.251.251.251"), default);

        Assert.IsNotNull(result);
        Assert.AreEqual("996 is Fubao", result.PostTitle);
        Assert.AreEqual(req.Email, result.Email);
        Assert.AreEqual(req.Content, result.CommentContent);
        Assert.AreEqual(req.Username, result.Username);
    }

    [Test]
    public async Task CreateAsync_HasBadWord_Block()
    {
        _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
        {
            EnableWordFilter = true,
            WordFilterMode = WordFilterMode.Block,
            RequireCommentReview = true
        });

        _mockCommentModerator.Setup(p => p.HasBadWord(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(true));

        CommentRequest req = new CommentRequest
        {
            Content = "Work 996 and get into ICU",
            Email = "worker@996.icu",
            Username = "Fubao Collector"
        };
        var handler = new CreateCommentCommandHandler(_mockBlogConfig.Object, _mockPostEntityRepo.Object,
            _mockCommentModerator.Object, _mockCommentEntityRepo.Object);

        var result = await handler.Handle(new(Guid.Empty, req, "251.251.251.251"), default);
        Assert.IsNull(result);
    }

    [Test]
    public void AddReply_NullComment()
    {
        _mockCommentEntityRepo.Setup(p => p.GetAsync(It.IsAny<Guid>())).Returns(ValueTask.FromResult((CommentEntity)null));

        var handler = new ReplyCommentCommandHandler(_mockCommentEntityRepo.Object, _mockCommentReplyEntityRepo.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Guid.Empty, "996"), default);
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetCommentsAsync_InvalidPageSize(int pageSize)
    {
        var handler = new GetCommentsQueryHandler(_mockCommentEntityRepo.Object);

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await handler.Handle(new(pageSize, 1), default);
        });
    }

    [Test]
    public async Task GetCommentsAsync_OK()
    {
        IReadOnlyList<CommentDetailedItem> details = new List<CommentDetailedItem>();

        _mockCommentEntityRepo.Setup(p => p.SelectAsync(It.IsAny<CommentSpec>(),
                It.IsAny<Expression<Func<CommentEntity, CommentDetailedItem>>>()))
            .Returns(Task.FromResult(details));

        var handler = new GetCommentsQueryHandler(_mockCommentEntityRepo.Object);
        var result = await handler.Handle(new(7, 996), default);

        Assert.IsNotNull(result);
        _mockCommentEntityRepo.Verify(p => p.SelectAsync(It.IsAny<CommentSpec>(),
            It.IsAny<Expression<Func<CommentEntity, CommentDetailedItem>>>()));
    }
}