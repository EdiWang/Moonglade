using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Web.Controllers;
using Moq;
using System.Net;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class ActivityLogControllerTests
{
    private readonly Mock<IQueryMediator> _queryMediator = new();
    private readonly RecordingCommandMediator _commandMediator = new();

    [Fact]
    public async Task GetEventTypes_ReturnsOkWithEventTypes()
    {
        var expected = new List<EventTypeGroup>
        {
            new()
            {
                Category = "Post",
                Items =
                [
                    new EventTypeItem
                    {
                        Value = (int)EventType.PostCreated,
                        Name = "Post Created"
                    }
                ]
            }
        };

        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetEventTypesQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = CreateController();

        var result = await controller.GetEventTypes();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task List_NormalizesPagingAndMapsEventTypes()
    {
        var startTimeUtc = DateTime.UtcNow.AddDays(-2);
        var endTimeUtc = DateTime.UtcNow;
        var logs = new List<ActivityLogItem>
        {
            new()
            {
                Id = 1,
                EventType = EventType.PostCreated,
                EventTimeUtc = DateTime.UtcNow,
                ActorId = "tester",
                Operation = "Create Post",
                TargetName = "Hello"
            }
        };

        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<ListActivityLogsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((logs, 25));

        var controller = CreateController();

        var result = await controller.List(0, 200, [(int)EventType.PostCreated, (int)EventType.CommentDeleted], startTimeUtc, endTimeUtc);

        _queryMediator.Verify(
            x => x.QueryAsync(
                It.Is<ListActivityLogsQuery>(query =>
                    query.PageIndex == 1 &&
                    query.PageSize == 100 &&
                    query.EventTypes != null &&
                    query.EventTypes.SequenceEqual(new[] { EventType.PostCreated, EventType.CommentDeleted }) &&
                    query.StartTimeUtc == startTimeUtc &&
                    query.EndTimeUtc == endTimeUtc),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<ActivityLogItem>>(okResult.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal(1, pagedResult.PageNumber);
        Assert.Equal(100, pagedResult.PageSize);
        Assert.Equal(25, pagedResult.TotalItemCount);
    }

    [Fact]
    public async Task GetMetadata_WhenMetadataDoesNotExist_ReturnsNotFound()
    {
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetMetaDataByActivityLogIdQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        var controller = CreateController();

        var result = await controller.GetMetadata(42);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetMetadata_WhenMetadataExists_ReturnsOk()
    {
        const string metadata = "{\"ActivityLogId\":42}";

        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetMetaDataByActivityLogIdQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        var controller = CreateController();

        var result = await controller.GetMetadata(42);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(metadata, okResult.Value);
    }

    [Fact]
    public async Task Delete_WhenActivityLogDoesNotExist_ReturnsNotFound()
    {
        _commandMediator.SetResult<DeleteActivityLogCommand, OperationCode>(OperationCode.ObjectNotFound);
        var controller = CreateController();

        var result = await controller.Delete(42);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(_commandMediator.Commands);
        Assert.IsType<DeleteActivityLogCommand>(_commandMediator.Commands[0]);
        Assert.Empty(_commandMediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Delete_WhenActivityLogExists_ReturnsNoContentAndWritesActivityLog()
    {
        _commandMediator.SetResult<DeleteActivityLogCommand, OperationCode>(OperationCode.Done);
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Delete(42);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(42, _commandMediator.Single<DeleteActivityLogCommand>().Id);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.ActivityLogDeleted, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Delete Activity Log", activityCommand.Operation);
        Assert.Equal("Activity Log #42", activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(42L, activityCommand.MetaData!.GetType().GetProperty("ActivityLogId")!.GetValue(activityCommand.MetaData));
    }

    private ActivityLogController CreateController(
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var controller = new ActivityLogController(_queryMediator.Object, _commandMediator);
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
