using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moq;

namespace Moonglade.Webmention.Tests;

public class ReceiveWebmentionCommandHandlerTests
{
    private readonly Mock<ILogger<ReceiveWebmentionCommandHandler>> _mockLogger;
    private readonly Mock<IMentionSourceInspector> _mockSourceInspector;
    private readonly Mock<IRepositoryBase<MentionEntity>> _mockMentionRepo;
    private readonly Mock<IRepositoryBase<PostEntity>> _mockPostRepo;
    private readonly ReceiveWebmentionCommandHandler _handler;

    public ReceiveWebmentionCommandHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ReceiveWebmentionCommandHandler>>();
        _mockSourceInspector = new Mock<IMentionSourceInspector>();
        _mockMentionRepo = new Mock<IRepositoryBase<MentionEntity>>();
        _mockPostRepo = new Mock<IRepositoryBase<PostEntity>>();

        _handler = new ReceiveWebmentionCommandHandler(
            _mockLogger.Object,
            _mockSourceInspector.Object,
            _mockMentionRepo.Object,
            _mockPostRepo.Object
        );
    }

    [Fact]
    public async Task HandleAsync_InvalidSourceUrl_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("invalid-url", "https://example.com/post", "192.168.1.1");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidTargetUrl_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("https://example.com/source", "invalid-url", "192.168.1.1");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_LoopbackSourceUrl_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("http://localhost/source", "https://example.com/post", "192.168.1.1");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PrivateIpSourceUrl_10Network_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("http://10.0.0.1/source", "https://example.com/post", "192.168.1.1");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PrivateIpSourceUrl_172Network_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("http://172.16.0.1/source", "https://example.com/post", "192.168.1.1");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PrivateIpSourceUrl_192Network_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("http://192.168.1.100/source", "https://example.com/post", "192.168.1.1");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NonHttpScheme_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("ftp://example.com/source", "https://example.com/post", "192.168.1.1");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExamineSourceReturnsNull_ReturnsInvalidWebmentionRequest()
    {
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post", "192.168.1.1");

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((MentionRequest)null!);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.InvalidWebmentionRequest, result.Status);
        _mockSourceInspector.Verify(x => x.ExamineSourceAsync("https://example.com/source", "https://myblog.com/post"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SourceDoesNotContainTarget_ReturnsErrorSourceNotContainTargetUri()
    {
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

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.ErrorSourceNotContainTargetUri, result.Status);
        _mockPostRepo.Verify(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ContainsHtml_ReturnsSpamDetectedFakeNotFound()
    {
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

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.SpamDetectedFakeNotFound, result.Status);
        _mockPostRepo.Verify(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_TargetPostNotFound_ReturnsErrorTargetUriNotExist()
    {
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

        _mockPostRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid.Empty, string.Empty));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.ErrorTargetUriNotExist, result.Status);
        _mockMentionRepo.Verify(x => x.AnyAsync(It.IsAny<MentionSpec>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateMention_ReturnsErrorWebmentionAlreadyRegistered()
    {
        var postId = Guid.NewGuid();
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

        _mockPostRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((postId, "Test Post Title"));

        _mockMentionRepo.Setup(x => x.AnyAsync(It.IsAny<MentionSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.ErrorWebmentionAlreadyRegistered, result.Status);
        _mockMentionRepo.Verify(x => x.AddAsync(It.IsAny<MentionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidWebmention_CreatesAndReturnsSuccess()
    {
        var postId = Guid.NewGuid();
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

        _mockPostRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((postId, "Test Post Title"));

        _mockMentionRepo.Setup(x => x.AnyAsync(It.IsAny<MentionSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        MentionEntity? capturedMention = null;
        _mockMentionRepo.Setup(x => x.AddAsync(It.IsAny<MentionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MentionEntity, CancellationToken>((mention, ct) => capturedMention = mention)
            .ReturnsAsync((MentionEntity m, CancellationToken _) => m);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.Success, result.Status);
        Assert.NotNull(result.MentionEntity);
        Assert.NotNull(capturedMention);
        Assert.Equal("https://example.com/source", capturedMention.SourceUrl);
        Assert.Equal("Source Page Title", capturedMention.SourceTitle);
        Assert.Equal(postId, capturedMention.TargetPostId);
        Assert.Equal("Test Post Title", capturedMention.TargetPostTitle);
        Assert.Equal("203.0.113.1", capturedMention.SourceIp);
        Assert.Equal("example.com", capturedMention.Domain);
        Assert.NotEqual(Guid.Empty, capturedMention.Id);

        _mockMentionRepo.Verify(x => x.AddAsync(It.IsAny<MentionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExceptionThrown_ReturnsGenericError()
    {
        var command = new ReceiveWebmentionCommand("https://example.com/source", "https://myblog.com/post", "192.168.1.1");

        _mockSourceInspector.Setup(x => x.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.GenericError, result.Status);
    }

    [Theory]
    [InlineData("http://example.com/source")]
    [InlineData("https://example.com/source")]
    public async Task HandleAsync_ValidHttpAndHttpsSchemes_ProcessesCorrectly(string sourceUrl)
    {
        var postId = Guid.NewGuid();
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

        _mockPostRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((postId, "Test Post Title"));

        _mockMentionRepo.Setup(x => x.AnyAsync(It.IsAny<MentionSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.Success, result.Status);
        Assert.NotNull(result.MentionEntity);
    }

    [Fact]
    public async Task HandleAsync_ValidWebmention_SetsCorrectPingTime()
    {
        var postId = Guid.NewGuid();
        var beforeTest = DateTime.UtcNow.AddSeconds(-1);
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

        _mockPostRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((postId, "Test Post Title"));

        _mockMentionRepo.Setup(x => x.AnyAsync(It.IsAny<MentionSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        MentionEntity? capturedMention = null;
        _mockMentionRepo.Setup(x => x.AddAsync(It.IsAny<MentionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MentionEntity, CancellationToken>((mention, ct) => capturedMention = mention)
            .ReturnsAsync((MentionEntity m, CancellationToken _) => m);

        var result = await _handler.HandleAsync(command, CancellationToken.None);
        var afterTest = DateTime.UtcNow.AddSeconds(1);

        Assert.Equal(WebmentionStatus.Success, result.Status);
        Assert.NotNull(capturedMention);
        Assert.True(capturedMention.PingTimeUtc >= beforeTest);
        Assert.True(capturedMention.PingTimeUtc <= afterTest);
    }

    [Theory]
    [InlineData("http://172.15.0.1/source")]   // Just outside 172.16.0.0/12 range
    [InlineData("http://172.32.0.1/source")]   // Just outside 172.16.0.0/12 range
    [InlineData("http://11.0.0.1/source")]     // Just outside 10.0.0.0/8 range
    [InlineData("http://192.167.1.1/source")]  // Just outside 192.168.0.0/16 range
    [InlineData("http://192.169.1.1/source")]  // Just outside 192.168.0.0/16 range
    public async Task HandleAsync_PublicIpAddresses_ProcessesCorrectly(string sourceUrl)
    {
        var postId = Guid.NewGuid();
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

        _mockPostRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByRouteLinkForIdTitleSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((postId, "Test Post Title"));

        _mockMentionRepo.Setup(x => x.AnyAsync(It.IsAny<MentionSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(WebmentionStatus.Success, result.Status);
    }
}
