using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

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
            .ReturnsAsync((string)null!);

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

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task Clear_WithRealHandlers_ReplacesHistoryWithCountedAdministratorAudit(int historyCount)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(historyCount, ct);
        using var scope = store.Services.CreateScope();
        var controller = RealController(scope.ServiceProvider);
        var before = DateTime.UtcNow;

        Assert.IsType<NoContentResult>(await controller.Clear());

        var row = Assert.Single(await store.ReadLogsAsync(ct));
        Assert.Equal((int)EventType.ActivityLogsCleared, row.EventId);
        Assert.Equal("admin", row.ActorId);
        Assert.Equal("Clear Activity Logs", row.Operation);
        Assert.Equal("Activity Logs", row.TargetName);
        Assert.Equal("192.0.2.25", row.IpAddress);
        Assert.Equal("private-agent-marker", row.UserAgent);
        Assert.Equal(DateTimeKind.Utc, row.EventTimeUtc!.Value.Kind);
        Assert.InRange(row.EventTimeUtc.Value, before, DateTime.UtcNow);
        using var metadata = JsonDocument.Parse(row.MetaData);
        Assert.Equal(historyCount, metadata.RootElement.GetProperty("DeletedCount").GetInt32());
    }

    [Fact]
    public async Task Clear_WhenDatabaseDeleteFails_PreservesRowsAndDoesNotAudit()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(3, ct);
        var original = await store.ReadLogsAsync(ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """CREATE TRIGGER fail_clear BEFORE DELETE ON ActivityLog BEGIN SELECT RAISE(ABORT, 'synthetic delete failure'); END;""", ct);

        var exception = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => RealController(scope.ServiceProvider).Clear());

        Assert.Contains("synthetic delete failure", exception.Message);
        var remaining = await store.ReadLogsAsync(ct);
        Assert.Equal(original.Select(row => (row.Id, row.Operation, row.EventId)),
            remaining.Select(row => (row.Id, row.Operation, row.EventId)));
        Assert.Empty(store.ControllerLogger.Invocations);
        Assert.Empty(store.WriterLogger.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Clear_WhenAuditDispatchFails_PreservesCommittedDeleteAndLogsSafely(bool cancellation)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(3, ct);
        using var scope = store.Services.CreateScope();
        var real = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
        var mediator = new Mock<ICommandMediator>();
        using var auditCancellation = new CancellationTokenSource();
        Exception failure = cancellation
            ? new OperationCanceledException("synthetic cancellation", auditCancellation.Token)
            : new InvalidOperationException("synthetic audit failure");
        mediator.Setup(value => value.SendAsync(It.IsAny<ClearActivityLogsCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((ClearActivityLogsCommand command, CommandMediationSettings? settings, CancellationToken token) =>
                real.SendAsync(command, settings, token));
        mediator.Setup(value => value.SendAsync(It.IsAny<ICommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns(async (ICommand command, CommandMediationSettings? _, CancellationToken _) =>
            {
                Assert.IsType<CreateActivityLogCommand>(command);
                Assert.Empty(await store.ReadLogsAsync(ct));
                if (cancellation) auditCancellation.Cancel();
                throw failure;
            });
        var controller = RealController(scope.ServiceProvider, mediator.Object);
        controller.HttpContext.RequestAborted = auditCancellation.Token;

        Assert.IsType<NoContentResult>(await controller.Clear());

        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Equal(cancellation, auditCancellation.IsCancellationRequested);
        var diagnostic = Assert.Single(store.ControllerLogger.Invocations);
        Assert.Equal(LogLevel.Error, diagnostic.Arguments[0]);
        Assert.Same(failure, diagnostic.Arguments[3]);
        var state = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object>>>(diagnostic.Arguments[2]);
        Assert.Contains(state, value => value.Key == "EventType" && Equals(value.Value, EventType.ActivityLogsCleared));
        Assert.Contains(state, value => value.Key == "Operation" && Equals(value.Value, "Clear Activity Logs"));
        Assert.Contains(state, value => value.Key == "TraceIdentifier" && Equals(value.Value, "b1-clear-trace"));
        var message = diagnostic.Arguments[2]!.ToString()!;
        Assert.DoesNotContain("private-agent-marker", message);
        Assert.DoesNotContain("192.0.2.25", message);
        Assert.DoesNotContain("DeletedCount", message);
    }

    [Fact]
    public async Task Clear_WhenAuditPersistenceFails_DoesNotResurrectOnNextSave()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(3, ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """CREATE TRIGGER fail_audit BEFORE INSERT ON ActivityLog BEGIN SELECT RAISE(ABORT, 'synthetic insert failure'); END;""", ct);
        var mediator = new Mock<ICommandMediator>();
        var clearHandler = new ClearActivityLogsCommandHandler(db, Mock.Of<ILogger<ClearActivityLogsCommandHandler>>());
        var writer = new CreateActivityLogCommandHandler(db, store.WriterLogger.Object);
        mediator.Setup(value => value.SendAsync(It.IsAny<ClearActivityLogsCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((ClearActivityLogsCommand command, CommandMediationSettings? _, CancellationToken token) =>
                clearHandler.HandleAsync(command, token));
        mediator.Setup(value => value.SendAsync(It.IsAny<ICommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((ICommand command, CommandMediationSettings? _, CancellationToken token) =>
                writer.HandleAsync(Assert.IsType<CreateActivityLogCommand>(command), token));
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "B1SyntheticSetting",
            CfgValue = "unrelated-save",
            LastModifiedTimeUtc = DateTime.UtcNow
        });

        Assert.IsType<NoContentResult>(await RealController(scope.ServiceProvider, mediator.Object).Clear());

        Assert.Empty(await store.ReadLogsAsync(ct));
        var diagnostic = Assert.Single(store.WriterLogger.Invocations);
        Assert.Equal(LogLevel.Error, diagnostic.Arguments[0]);
        var failure = Assert.IsType<DbUpdateException>(diagnostic.Arguments[3]);
        Assert.Contains("synthetic insert failure", failure.InnerException!.Message);
        Assert.Empty(await db.BlogConfiguration.AsNoTracking().ToListAsync(ct));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_audit;", ct);
        await db.SaveChangesAsync(ct);

        Assert.Equal("unrelated-save", (await db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
        Assert.Empty(await store.ReadLogsAsync(ct));
    }

    [Fact]
    public async Task Clear_ClearsActivityLogsAndWritesActivityLog()
    {
        _commandMediator.SetResult<ClearActivityLogsCommand, int>(7);
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Clear();

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<ClearActivityLogsCommand>(_commandMediator.Commands[0]);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.ActivityLogsCleared, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Clear Activity Logs", activityCommand.Operation);
        Assert.Equal("Activity Logs", activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(7, ActivityLogMetaDataAssert.Value<int>(activityCommand, "DeletedCount"));
    }

    private ActivityLogController CreateController(
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var controller = new ActivityLogController(_queryMediator.Object, _commandMediator, Mock.Of<ILogger<ActivityLogController>>());
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

    private static ActivityLogController RealController(IServiceProvider services, ICommandMediator? mediator = null) =>
        new(services.GetRequiredService<IQueryMediator>(),
            mediator ?? services.GetRequiredService<ICommandMediator>(),
            services.GetRequiredService<ILogger<ActivityLogController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "b1-clear-trace",
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "TestAuth")),
                    Connection = { RemoteIpAddress = IPAddress.Parse("192.0.2.25") },
                    Request = { Headers = { UserAgent = "private-agent-marker" } }
                }
            }
        };

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        private readonly Dictionary<Type, object> _results = [];

        public List<ICommand> Commands { get; } = [];

        public Task<MediationResult> TrySendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationResult<TCommandResult>> TrySendAsync<TCommandResult>(ICommand<TCommandResult> command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationDecision> EvaluateAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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
