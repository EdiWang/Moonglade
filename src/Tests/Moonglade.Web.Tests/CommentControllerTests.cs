using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.Data.DTO;
using Moonglade.Features.Comment;
using Moonglade.Moderation;
using Moonglade.Web.Controllers;
using Moonglade.Web.Services;
using Moq;
using System.Net;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class CommentControllerTests
{
    private readonly BlogConfig _blogConfig = new()
    {
        CommentSettings = new CommentSettings
        {
            EnableComments = true,
            RequireCommentReview = false
        },
        NotificationSettings = new NotificationSettings()
    };
    private readonly RecordingCommandMediator _commandMediator = new();
    private readonly Mock<IQueryMediator> _queryMediator = new();
    private readonly Mock<IModeratorService> _moderator = new();
    private readonly Mock<ICommentSubmissionGuard> _submissionGuard = new();

    public CommentControllerTests()
    {
        _submissionGuard
            .Setup(x => x.Validate(It.IsAny<CommentRequest>()))
            .Returns(CommentSubmissionGuardResult.Success);
    }

    [Fact]
    public async Task Create_WhenCommentsDisabled_ReturnsForbid()
    {
        _blogConfig.CommentSettings.EnableComments = false;
        var controller = CreateController();

        var result = await controller.Create(Guid.NewGuid(), CreateCommentRequest());

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Create_WhenEmailIsInvalid_ReturnsValidationProblem()
    {
        var controller = CreateController();

        var result = await controller.Create(Guid.NewGuid(), CreateCommentRequest("bad-email"));

        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(problemResult.Value);
        Assert.True(controller.ModelState.ContainsKey(nameof(CommentRequest.Email)));
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Create_WhenSubmissionGuardRejectsRequest_ReturnsValidationProblem()
    {
        _submissionGuard
            .Setup(x => x.Validate(It.IsAny<CommentRequest>()))
            .Returns(CommentSubmissionGuardResult.Failure(nameof(CommentRequest.FormRenderedUtc), "Invalid comment submission."));
        var controller = CreateController();

        var result = await controller.Create(Guid.NewGuid(), CreateCommentRequest());

        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(problemResult.Value);
        Assert.True(controller.ModelState.ContainsKey(nameof(CommentRequest.FormRenderedUtc)));
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Create_WhenBlockFilterDetectsContent_ReturnsValidationProblem()
    {
        _blogConfig.CommentSettings.EnableWordFilter = true;
        _blogConfig.CommentSettings.WordFilterMode = WordFilterMode.Block;
        _moderator
            .Setup(x => x.Detect("reader", "Hello world"))
            .ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Create(Guid.NewGuid(), CreateCommentRequest());

        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(problemResult.Value);
        Assert.True(controller.ModelState.ContainsKey(nameof(CommentRequest.Content)));
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Create_WhenMaskFilterEnabled_MasksRequestAndWritesActivityLog()
    {
        var postId = Guid.NewGuid();
        var request = CreateCommentRequest();
        var comment = new CommentDetailedItem
        {
            Id = Guid.NewGuid(),
            Username = "masked-reader",
            Email = request.Email,
            IpAddress = "127.0.0.1",
            PostTitle = "Hello Post",
            CommentContent = "masked-content",
            CreateTimeUtc = DateTime.UtcNow,
            IsApproved = true
        };
        _blogConfig.CommentSettings.EnableWordFilter = true;
        _blogConfig.CommentSettings.WordFilterMode = WordFilterMode.Mask;
        _blogConfig.CommentSettings.RequireCommentReview = false;
        _commandMediator.SetResult<CreateCommentCommand, CommentDetailedItem>(comment);
        _moderator.Setup(x => x.Mask("reader")).ReturnsAsync("masked-reader");
        _moderator.Setup(x => x.Mask("Hello world")).ReturnsAsync("masked-content");
        var controller = CreateController(remoteIpAddress: IPAddress.Parse("127.0.0.1"), userAgent: "unit-test-agent");

        var result = await controller.Create(postId, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.False((bool)okResult.Value!.GetType().GetProperty(nameof(CommentSettings.RequireCommentReview))!.GetValue(okResult.Value)!);
        Assert.True((long)okResult.Value.GetType().GetProperty(nameof(CommentRequest.FormRenderedUtc))!.GetValue(okResult.Value)! > 0);
        var command = _commandMediator.Single<CreateCommentCommand>();
        Assert.Equal(postId, command.PostId);
        Assert.Same(request, command.Payload);
        Assert.Equal("masked-reader", command.Payload.Username);
        Assert.Equal("masked-content", command.Payload.Content);
        Assert.Equal("127.0.0.1", command.IpAddress);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.CommentCreated, activityCommand.EventType);
        Assert.Equal(comment.Username, activityCommand.ActorId);
        Assert.Equal("Create Comment", activityCommand.Operation);
        Assert.Equal(comment.PostTitle, activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(comment.Id, activityCommand.MetaData!.GetType().GetProperty("CommentId")!.GetValue(activityCommand.MetaData));
        Assert.Equal(comment.Username, activityCommand.MetaData.GetType().GetProperty(nameof(comment.Username))!.GetValue(activityCommand.MetaData));
        Assert.Equal(postId, activityCommand.MetaData.GetType().GetProperty("PostId")!.GetValue(activityCommand.MetaData));
    }

    [Fact]
    public async Task Create_WhenCommandReturnsNull_ReturnsValidationProblem()
    {
        _commandMediator.SetResult<CreateCommentCommand, CommentDetailedItem>(null!);
        var controller = CreateController();

        var result = await controller.Create(Guid.NewGuid(), CreateCommentRequest());

        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(problemResult.Value);
        Assert.True(controller.ModelState.ContainsKey("postId"));
        Assert.Empty(_commandMediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Approval_WhenCommentExists_ReturnsOkAndWritesActivityLog()
    {
        var commentId = Guid.NewGuid();
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Approval(commentId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(commentId, okResult.Value);
        Assert.Equal(commentId, _commandMediator.Single<ToggleApprovalCommand>().CommentIds.Single());

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.CommentApprovalToggled, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Toggle Comment Approval", activityCommand.Operation);
        Assert.Equal($"Comment #{commentId}", activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(commentId, activityCommand.MetaData!.GetType().GetProperty("CommentId")!.GetValue(activityCommand.MetaData));
    }

    [Fact]
    public async Task Approval_WhenCommandThrowsArgumentException_ReturnsNotFound()
    {
        var commentId = Guid.NewGuid();
        _commandMediator.SetException<ToggleApprovalCommand>(new ArgumentException("missing"));
        var controller = CreateController();

        var result = await controller.Approval(commentId);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Comment with ID {commentId} not found.", notFoundResult.Value);
        Assert.Empty(_commandMediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Delete_WhenCommentsExist_ReturnsOkAndWritesActivityLog()
    {
        var commentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Delete(commentIds);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(commentIds, okResult.Value);
        Assert.Same(commentIds, _commandMediator.Single<DeleteCommentsCommand>().Ids);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.CommentDeleted, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Delete Comments", activityCommand.Operation);
        Assert.Equal("2 comment(s)", activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Same(commentIds, activityCommand.MetaData!.GetType().GetProperty("CommentIds")!.GetValue(activityCommand.MetaData));
    }

    [Fact]
    public async Task Delete_WhenCommandThrowsArgumentException_ReturnsNotFound()
    {
        const string errorMessage = "missing comments";
        var commentIds = new[] { Guid.NewGuid() };
        _commandMediator.SetException<DeleteCommentsCommand>(new ArgumentException(errorMessage));
        var controller = CreateController();

        var result = await controller.Delete(commentIds);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(errorMessage, notFoundResult.Value);
        Assert.Empty(_commandMediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task List_ReturnsPagedCommentsWithHtmlContent()
    {
        var filter = new CommentFilter { Username = "reader" };
        var commentId = Guid.NewGuid();
        var createTimeUtc = DateTime.UtcNow.AddHours(-1);
        var replyTimeUtc = DateTime.UtcNow;
        var comments = new List<CommentDetailedItem>
        {
            new()
            {
                Id = commentId,
                Username = "reader",
                Email = "reader@example.com",
                CreateTimeUtc = createTimeUtc,
                CommentContent = "**Hello**",
                IpAddress = "127.0.0.1",
                PostTitle = "Hello Post",
                IsApproved = true,
                Replies =
                [
                    new CommentReplyDigest
                    {
                        ReplyTimeUtc = replyTimeUtc,
                        ReplyContent = "**Reply**"
                    }
                ]
            }
        };
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<ListCommentsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<CountCommentsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        var controller = CreateController();

        var result = await controller.List(2, 10, filter);

        _queryMediator.Verify(
            x => x.QueryAsync(
                It.Is<ListCommentsQuery>(query => query.PageIndex == 2 && query.PageSize == 10 && ReferenceEquals(query.Filter, filter)),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _queryMediator.Verify(
            x => x.QueryAsync(
                It.Is<CountCommentsQuery>(query => ReferenceEquals(query.Filter, filter)),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<object>>(okResult.Value);
        Assert.Equal(2, pagedResult.PageNumber);
        Assert.Equal(10, pagedResult.PageSize);
        Assert.Equal(12, pagedResult.TotalItemCount);

        var item = Assert.Single(pagedResult.Items);
        Assert.Equal(commentId, item.GetType().GetProperty(nameof(CommentDetailedItem.Id))!.GetValue(item));
        Assert.Equal("reader", item.GetType().GetProperty(nameof(CommentDetailedItem.Username))!.GetValue(item));
        Assert.Contains("<strong>Hello</strong>", (string)item.GetType().GetProperty(nameof(CommentDetailedItem.CommentContent))!.GetValue(item)!);
        var replies = Assert.IsAssignableFrom<IEnumerable<object>>(item.GetType().GetProperty("Replies")!.GetValue(item));
        var reply = Assert.Single(replies);
        Assert.Equal("**Reply**", reply.GetType().GetProperty(nameof(CommentReplyDigest.ReplyContent))!.GetValue(reply));
        Assert.Contains("<strong>Reply</strong>", (string)reply.GetType().GetProperty("ReplyContentHtml")!.GetValue(reply)!);
    }

    [Fact]
    public async Task List_WhenFilterIsNull_UsesDefaultFilter()
    {
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<ListCommentsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<CountCommentsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var controller = CreateController();

        var result = await controller.List();

        Assert.IsType<OkObjectResult>(result);
        _queryMediator.Verify(
            x => x.QueryAsync(
                It.Is<ListCommentsQuery>(query => query.PageIndex == 1 && query.PageSize == 5 && query.Filter != null),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _queryMediator.Verify(
            x => x.QueryAsync(
                It.Is<CountCommentsQuery>(query => query.Filter != null),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reply_WhenReplyContentIsEmpty_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Reply(Guid.NewGuid(), " ");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Reply content cannot be empty.", badRequestResult.Value);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Reply_WhenCommentsDisabled_ReturnsForbid()
    {
        _blogConfig.CommentSettings.EnableComments = false;
        var controller = CreateController();

        var result = await controller.Reply(Guid.NewGuid(), "Thanks");

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Reply_WhenCommentExists_ReturnsOkAndWritesActivityLog()
    {
        var commentId = Guid.NewGuid();
        var reply = new CommentReply
        {
            Id = Guid.NewGuid(),
            CommentId = commentId,
            Email = "reader@example.com",
            CommentContent = "Original",
            Title = "Hello Post",
            ReplyContent = "Thanks",
            ReplyContentHtml = "<p>Thanks</p>",
            ReplyTimeUtc = DateTime.UtcNow,
            RouteLink = "hello-post"
        };
        _commandMediator.SetResult<ReplyCommentCommand, CommentReply>(reply);
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Reply(commentId, "Thanks");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(reply, okResult.Value);
        var command = _commandMediator.Single<ReplyCommentCommand>();
        Assert.Equal(commentId, command.CommentId);
        Assert.Equal("Thanks", command.ReplyContent);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.CommentReplied, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Reply to Comment", activityCommand.Operation);
        Assert.Equal(reply.Title, activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(commentId, activityCommand.MetaData!.GetType().GetProperty("CommentId")!.GetValue(activityCommand.MetaData));
        Assert.Equal("Thanks", activityCommand.MetaData.GetType().GetProperty("ReplyContent")!.GetValue(activityCommand.MetaData));
    }

    [Fact]
    public async Task Reply_WhenCommandThrowsArgumentException_ReturnsNotFound()
    {
        var commentId = Guid.NewGuid();
        _commandMediator.SetException<ReplyCommentCommand>(new ArgumentException("missing"));
        var controller = CreateController();

        var result = await controller.Reply(commentId, "Thanks");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Comment with ID {commentId} not found.", notFoundResult.Value);
        Assert.Empty(_commandMediator.Commands.OfType<CreateActivityLogCommand>());
    }

    private CommentController CreateController(
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var controller = new CommentController(
            _commandMediator,
            _queryMediator.Object,
            _moderator.Object,
            _blogConfig,
            _submissionGuard.Object,
            CreateCannonService());
        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(username))
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], "TestAuth"));
        }

        if (remoteIpAddress is not null)
        {
            httpContext.Connection.RemoteIpAddress = remoteIpAddress;
        }

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            httpContext.Request.Headers.UserAgent = userAgent;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static CannonService CreateCannonService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IEventMediator>());
        return new CannonService(Mock.Of<ILogger<CannonService>>(), services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
    }

    private static CommentRequest CreateCommentRequest(string email = "reader@example.com") => new()
    {
        Username = "reader",
        Content = "Hello world",
        Email = email,
        Source = string.Empty,
        FormRenderedUtc = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeMilliseconds(),
        CaptchaCode = "1234",
        CaptchaToken = "token"
    };

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        private readonly Dictionary<Type, object> _results = [];
        private readonly Dictionary<Type, Exception> _exceptions = [];

        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken)
        {
            Commands.Add(command);

            if (_exceptions.TryGetValue(command.GetType(), out var exception))
            {
                throw exception;
            }

            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings? settings,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);

            if (_exceptions.TryGetValue(command.GetType(), out var exception))
            {
                throw exception;
            }

            return Task.FromResult((TCommandResult)_results[command.GetType()]);
        }

        public void SetResult<TCommand, TResult>(TResult result) where TCommand : ICommand<TResult>
        {
            _results[typeof(TCommand)] = result!;
        }

        public void SetException<TCommand>(Exception exception) where TCommand : ICommand
        {
            _exceptions[typeof(TCommand)] = exception;
        }

        public TCommand Single<TCommand>() where TCommand : ICommand
        {
            return Commands.OfType<TCommand>().Single();
        }
    }
}
