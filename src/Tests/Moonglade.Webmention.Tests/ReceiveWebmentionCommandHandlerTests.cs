using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Webmention.Tests;

public class ReceiveWebmentionCommandHandlerTests
{
    private readonly Mock<ILogger<ReceiveWebmentionCommandHandler>> _mockLogger;
    private readonly Mock<IMentionSourceInspector> _mockSourceInspector;

    public ReceiveWebmentionCommandHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ReceiveWebmentionCommandHandler>>();
        _mockSourceInspector = new Mock<IMentionSourceInspector>();
    }

    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    private ReceiveWebmentionCommandHandler CreateHandler(BlogDbContext db)
    {
        return new ReceiveWebmentionCommandHandler(
            _mockLogger.Object,
            _mockSourceInspector.Object,
            db
        );
    }

    [Fact]
    public async Task HandleAsync_InvalidSourceUrl_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("invalid-url", "https://example.com/post", "192.168.1.1");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidTargetUrl_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "invalid-url", "192.168.1.1");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_LoopbackSourceUrl_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("http://localhost/source", "https://example.com/post", "192.168.1.1");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PrivateIpSourceUrl_10Network_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("http://10.0.0.1/source", "https://example.com/post", "192.168.1.1");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PrivateIpSourceUrl_172Network_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("http://172.16.0.1/source", "https://example.com/post", "192.168.1.1");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PrivateIpSourceUrl_192Network_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("http://192.168.1.100/source", "https://example.com/post", "192.168.1.1");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NonHttpScheme_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("ftp://example.com/source", "https://example.com/post", "192.168.1.1");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExamineSourceReturnsNull_ReturnsInvalidWebmentionRequest()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post", "192.168.1.1");

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((MentionRequest)null!);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync("https://example.com/source", "https://myblog.com/post"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SourceDoesNotContainTarget_ReturnsErrorSourceNotContainTargetUri()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post", "192.168.1.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = "https://example.com/source",
            TargetUrl = "https://myblog.com/post",
            Title = "Test Title",
            ContainsHtml = false,
            SourceHasTarget = false
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mentionRequest);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.ErrorSourceNotContainTargetUri, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ContainsHtml_ReturnsSpamDetectedFakeNotFound()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post", "192.168.1.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = "https://example.com/source",
            TargetUrl = "https://myblog.com/post",
            Title = "Test Title",
            ContainsHtml = true,
            SourceHasTarget = true
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mentionRequest);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.SpamDetectedFakeNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_TargetPostNotFound_ReturnsErrorTargetUriNotExist()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post/2024/1/15/test-post", "192.168.1.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = "https://example.com/source",
            TargetUrl = "https://myblog.com/post/2024/1/15/test-post",
            Title = "Test Title",
            ContainsHtml = false,
            SourceHasTarget = true
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mentionRequest);

        // No post seeded — so FindTargetPostAsync will return Guid.Empty
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.ErrorTargetUriNotExist, result.Status);
    }

    [Fact]
    public async Task HandleAsync_DuplicateMention_ReturnsErrorWebmentionAlreadyRegistered()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();

        // Seed a published post with matching route link
        db.Post.Add(new PostEntity
        {
            Id = postId,
            Title = "Test Post Title",
            RouteLink = "2024/1/15/test-post",
            PostStatus = PostStatus.Published,
            IsDeleted = false,
            Slug = "test-post",
            ContentAbstract = "abstract",
            PostContent = "content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow,
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow
        });

        // Seed existing duplicate mention
        db.Mention.Add(new MentionEntity
        {
            Id = Guid.NewGuid(),
            Domain = "example.com",
            SourceUrl = "https://example.com/source",
            SourceTitle = "Test Title",
            SourceIp = "192.168.1.1",
            TargetPostId = postId,
            PingTimeUtc = DateTime.UtcNow,
            TargetPostTitle = "Test Post Title"
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post/2024/1/15/test-post", "192.168.1.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = "https://example.com/source",
            TargetUrl = "https://myblog.com/post/2024/1/15/test-post",
            Title = "Test Title",
            ContainsHtml = false,
            SourceHasTarget = true
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mentionRequest);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.ErrorWebmentionAlreadyRegistered, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ValidWebmention_CreatesAndReturnsSuccess()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();

        db.Post.Add(new PostEntity
        {
            Id = postId,
            Title = "Test Post Title",
            RouteLink = "2024/1/15/test-post",
            PostStatus = PostStatus.Published,
            IsDeleted = false,
            Slug = "test-post",
            ContentAbstract = "abstract",
            PostContent = "content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow,
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post/2024/1/15/test-post", "203.0.113.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = "https://example.com/source",
            TargetUrl = "https://myblog.com/post/2024/1/15/test-post",
            Title = "Source Page Title",
            ContainsHtml = false,
            SourceHasTarget = true
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync("https://example.com/source", "https://myblog.com/post/2024/1/15/test-post"))
            .ReturnsAsync(mentionRequest);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.Success, result.Status);
        Assert.NotNull(result.MentionEntity);
        Assert.Equal("https://example.com/source", result.MentionEntity.SourceUrl);
        Assert.Equal("Source Page Title", result.MentionEntity.SourceTitle);
        Assert.Equal(postId, result.MentionEntity.TargetPostId);
        Assert.Equal("Test Post Title", result.MentionEntity.TargetPostTitle);
        Assert.Equal("203.0.113.1", result.MentionEntity.SourceIp);
        Assert.Equal("example.com", result.MentionEntity.Domain);
        Assert.NotEqual(Guid.Empty, result.MentionEntity.Id);

        // Verify it was persisted
        Assert.Equal(1, await db.Mention.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_ExceptionThrown_ReturnsGenericError()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post", "192.168.1.1");

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.GenericError, result.Status);
    }

    [Theory]
    [InlineData("http://example.com/source")]
    [InlineData("https://example.com/source")]
    public async Task HandleAsync_ValidHttpAndHttpsSchemes_ProcessesCorrectly(string sourceUrl)
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();

        db.Post.Add(new PostEntity
        {
            Id = postId,
            Title = "Test Post Title",
            RouteLink = "2024/1/15/test-post",
            PostStatus = PostStatus.Published,
            IsDeleted = false,
            Slug = "test-post",
            ContentAbstract = "abstract",
            PostContent = "content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow,
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand(sourceUrl, "https://myblog.com/post/2024/1/15/test-post", "203.0.113.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = sourceUrl,
            TargetUrl = "https://myblog.com/post/2024/1/15/test-post",
            Title = "Source Page Title",
            ContainsHtml = false,
            SourceHasTarget = true
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mentionRequest);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.Success, result.Status);
        Assert.NotNull(result.MentionEntity);
    }

    [Fact]
    public async Task HandleAsync_ValidWebmention_SetsCorrectPingTime()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var beforeTest = DateTime.UtcNow.AddSeconds(-1);

        db.Post.Add(new PostEntity
        {
            Id = postId,
            Title = "Test Post Title",
            RouteLink = "2024/1/15/test-post",
            PostStatus = PostStatus.Published,
            IsDeleted = false,
            Slug = "test-post",
            ContentAbstract = "abstract",
            PostContent = "content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow,
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post/2024/1/15/test-post", "203.0.113.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = "https://example.com/source",
            TargetUrl = "https://myblog.com/post/2024/1/15/test-post",
            Title = "Source Page Title",
            ContainsHtml = false,
            SourceHasTarget = true
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mentionRequest);

        var result = await handler.HandleAsync(command, CancellationToken.None);
        var afterTest = DateTime.UtcNow.AddSeconds(1);

        Assert.Equal(WebmentionStatus.Success, result.Status);
        Assert.NotNull(result.MentionEntity);
        Assert.True(result.MentionEntity.PingTimeUtc >= beforeTest);
        Assert.True(result.MentionEntity.PingTimeUtc <= afterTest);
    }

    [Theory]
    [InlineData("http://172.15.0.1/source")]   // Just outside 172.16.0.0/12 range
    [InlineData("http://172.32.0.1/source")]   // Just outside 172.16.0.0/12 range
    [InlineData("http://11.0.0.1/source")]     // Just outside 10.0.0.0/8 range
    [InlineData("http://192.167.1.1/source")]  // Just outside 192.168.0.0/16 range
    [InlineData("http://192.169.1.1/source")]  // Just outside 192.168.0.0/16 range
    public async Task HandleAsync_PublicIpAddresses_ProcessesCorrectly(string sourceUrl)
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();

        db.Post.Add(new PostEntity
        {
            Id = postId,
            Title = "Test Post Title",
            RouteLink = "2024/1/15/test-post",
            PostStatus = PostStatus.Published,
            IsDeleted = false,
            Slug = "test-post",
            ContentAbstract = "abstract",
            PostContent = "content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow,
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var command = new ReceiveWebmentionCommand(sourceUrl, "https://myblog.com/post/2024/1/15/test-post", "203.0.113.1");

        var mentionRequest = new MentionRequest
        {
            SourceUrl = sourceUrl,
            TargetUrl = "https://myblog.com/post/2024/1/15/test-post",
            Title = "Source Page Title",
            ContainsHtml = false,
            SourceHasTarget = true
        };

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mentionRequest);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.Success, result.Status);
    }
}
