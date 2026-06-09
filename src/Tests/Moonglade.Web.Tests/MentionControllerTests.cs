using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Web.Controllers;
using Moonglade.Webmention;
using Moq;
using System.Net;

namespace Moonglade.Web.Tests;

public class MentionControllerTests
{
    private readonly BlogConfig _blogConfig = new()
    {
        AdvancedSettings = new AdvancedSettings
        {
            EnableWebmention = true
        }
    };
    private readonly Mock<IQueryMediator> _queryMediator = new();
    private readonly Mock<IEventMediator> _eventMediator = new();
    private readonly RecordingCommandMediator _commandMediator = new();

    [Fact]
    public async Task ReceiveWebmention_WhenWebmentionDisabled_ReturnsForbid()
    {
        _blogConfig.AdvancedSettings.EnableWebmention = false;
        var controller = CreateController();

        var result = await controller.ReceiveWebmention("https://example.com/source", "https://example.com/target");

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task ReceiveWebmention_WhenSuccessful_ReturnsOkAndQueuesMentionEmail()
    {
        var mention = CreateMention();
        _commandMediator.SetResult<ReceiveWebmentionCommand, WebmentionResponse>(new(WebmentionStatus.Success)
        {
            MentionEntity = mention
        });
        var controller = CreateController(IPAddress.Parse("127.0.0.1"));

        var result = await controller.ReceiveWebmention("https://source.example/post", "https://target.example/post");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Webmention received and verified.", okResult.Value);
        var command = _commandMediator.Single<ReceiveWebmentionCommand>();
        Assert.Equal("https://source.example/post", command.Source);
        Assert.Equal("https://target.example/post", command.Target);
        Assert.Equal("127.0.0.1", command.RemoteIp);
    }

    [Theory]
    [InlineData(WebmentionStatus.InvalidWebmentionRequest, typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest, "Invalid webmention request.")]
    [InlineData(WebmentionStatus.ErrorSourceNotContainTargetUri, typeof(ConflictObjectResult), StatusCodes.Status409Conflict, "The source URI does not contain a link to the target URI.")]
    [InlineData(WebmentionStatus.SpamDetectedFakeNotFound, typeof(NotFoundObjectResult), StatusCodes.Status404NotFound, "The requested resource was not found.")]
    [InlineData(WebmentionStatus.ErrorTargetUriNotExist, typeof(ConflictObjectResult), StatusCodes.Status409Conflict, "Cannot retrieve post ID and title for the target URL.")]
    [InlineData(WebmentionStatus.ErrorWebmentionAlreadyRegistered, typeof(ConflictObjectResult), StatusCodes.Status409Conflict, "Webmention already registered.")]
    [InlineData(WebmentionStatus.SourceRateLimitExceeded, typeof(ObjectResult), StatusCodes.Status429TooManyRequests, "Webmention source rate limit exceeded.")]
    [InlineData(WebmentionStatus.GenericError, typeof(ObjectResult), StatusCodes.Status500InternalServerError, "An internal server error occurred.")]
    public async Task ReceiveWebmention_WhenCommandReturnsFailure_ReturnsMappedResult(
        WebmentionStatus status,
        Type resultType,
        int statusCode,
        string message)
    {
        _commandMediator.SetResult<ReceiveWebmentionCommand, WebmentionResponse>(new(status));
        var controller = CreateController();

        var result = await controller.ReceiveWebmention("https://source.example/post", "https://target.example/post");

        Assert.IsType(resultType, result);
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(statusCode, objectResult.StatusCode);
        Assert.Equal(message, objectResult.Value);
    }

    [Fact]
    public async Task ListMentions_ReturnsPagedMentionsAndClampsPaging()
    {
        var mentions = new List<MentionEntity>
        {
            CreateMention()
        };
        var startTimeUtc = DateTime.UtcNow.AddDays(-1);
        var endTimeUtc = DateTime.UtcNow;
        _queryMediator
            .Setup(x => x.QueryAsync(
                It.IsAny<ListMentionsQuery>(),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((mentions, 101));
        var controller = CreateController();

        var result = await controller.ListMentions(
            pageIndex: 0,
            pageSize: 200,
            domain: "source.example",
            sourceTitle: "Source",
            targetPostTitle: "Target",
            startTimeUtc: startTimeUtc,
            endTimeUtc: endTimeUtc,
            sortBy: "sourceTitle",
            sortDescending: false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<MentionEntity>>(okResult.Value);
        Assert.Same(mentions.Single(), pagedResult.Items.Single());
        Assert.Equal(1, pagedResult.PageNumber);
        Assert.Equal(100, pagedResult.PageSize);
        Assert.Equal(101, pagedResult.TotalItemCount);
        Assert.Equal(2, pagedResult.PageCount);
        _queryMediator.Verify(
            x => x.QueryAsync(
                It.Is<ListMentionsQuery>(query =>
                    query.PageIndex == 1 &&
                    query.PageSize == 100 &&
                    query.Domain == "source.example" &&
                    query.SourceTitle == "Source" &&
                    query.TargetPostTitle == "Target" &&
                    query.StartTimeUtc == startTimeUtc &&
                    query.EndTimeUtc == endTimeUtc &&
                    query.SortBy == "sourceTitle" &&
                    !query.SortDescending),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task Delete_WhenIdsAreMissing_ReturnsBadRequest(int? idCount)
    {
        var ids = idCount.HasValue ? new List<Guid>() : null;
        var controller = CreateController();

        var result = await controller.Delete(ids);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No mention IDs provided.", badRequestResult.Value);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Delete_WhenIdsProvided_SendsDeleteCommandAndReturnsNoContent()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var controller = CreateController();

        var result = await controller.Delete(ids);

        Assert.IsType<NoContentResult>(result);
        Assert.Same(ids, _commandMediator.Single<DeleteMentionsCommand>().Ids);
    }

    [Fact]
    public async Task Clear_SendsClearCommandAndReturnsNoContent()
    {
        var controller = CreateController();

        var result = await controller.Clear();

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<ClearMentionsCommand>(_commandMediator.Commands.Single());
    }

    private MentionController CreateController(IPAddress? remoteIpAddress = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_eventMediator.Object);

        var controller = new MentionController(
            _blogConfig,
            _queryMediator.Object,
            new CannonService(Mock.Of<ILogger<CannonService>>(), services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>()),
            _commandMediator);

        var httpContext = new DefaultHttpContext();
        if (remoteIpAddress is not null)
        {
            httpContext.Connection.RemoteIpAddress = remoteIpAddress;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static MentionEntity CreateMention() => new()
    {
        Id = Guid.NewGuid(),
        Domain = "source.example",
        SourceUrl = "https://source.example/post",
        SourceTitle = "Source Post",
        SourceIp = "127.0.0.1",
        TargetPostId = Guid.NewGuid(),
        TargetPostTitle = "Target Post",
        PingTimeUtc = DateTime.UtcNow
    };

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        private readonly Dictionary<Type, object> _results = [];

        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings? settings,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.FromResult((TCommandResult)_results[command.GetType()]);
        }

        public void SetResult<TCommand, TResult>(TResult result) where TCommand : ICommand<TResult>
        {
            _results[typeof(TCommand)] = result!;
        }

        public TCommand Single<TCommand>() where TCommand : ICommand
        {
            return Commands.OfType<TCommand>().Single();
        }
    }
}
