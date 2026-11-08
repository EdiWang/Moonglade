using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Features.Post;
using Moonglade.Web.Commands;
using Moq;
using System.Text.Json;

namespace Moonglade.Web.Tests.Commands;

public class PostManagementCommandTests
{
    private readonly RecordingCommandMediator _commandMediator = new();

    [Fact]
    public async Task SavePostCommand_CreatePost_WritesActivityLog()
    {
        var handler = CreateSavePostHandler();
        var model = CreatePostEditModel(PostStatus.Draft);
        var postId = Guid.NewGuid();
        var routeLink = "2026/5/21/test-post";

        _commandMediator.SetResult<CreatePostCommand, PostCommandResult>(new PostCommandResult
        {
            Id = postId,
            RouteLink = routeLink,
            PostContent = model.EditorContent,
            LastModifiedUtc = DateTime.UtcNow
        });

        var result = await handler.HandleAsync(new SavePostCommand(model, null, CreateContext()), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(postId, result.PostId);
        var createCommand = _commandMediator.Single<CreatePostCommand>();
        Assert.Same(model, createCommand.Payload);
        var activityLogCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.PostCreated, activityLogCommand.EventKey);
        Assert.Equal(postId.ToString(), activityLogCommand.TargetId);
        Assert.Equal(model.Title, activityLogCommand.TargetDisplayName);
    }

    [Fact]
    public async Task SavePostCommand_UpdateScheduledPost_ConvertsClientTimeAndWakesPublisher()
    {
        var wakeUp = new ScheduledPublishWakeUp();
        var wakeToken = wakeUp.GetWakeToken();
        var handler = CreateSavePostHandler(wakeUp: wakeUp);
        var postId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddHours(2);
        var model = CreatePostEditModel(PostStatus.Scheduled);
        model.PostId = postId;
        model.ScheduledPublishTimeUtc = scheduledTime;

        _commandMediator.SetResult<UpdatePostCommand, PostCommandResult>(new PostCommandResult
        {
            Id = postId,
            PostContent = model.EditorContent,
            LastModifiedUtc = DateTime.UtcNow
        });
        _commandMediator.BeforeSend = command =>
        {
            if (command is UpdatePostCommand)
            {
                Assert.False(wakeToken.IsCancellationRequested);
            }
        };

        var result = await handler.HandleAsync(new SavePostCommand(model, null, CreateContext()), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(wakeToken.IsCancellationRequested);
        var updateCommand = _commandMediator.Single<UpdatePostCommand>();
        Assert.Equal(postId, updateCommand.Id);
        Assert.Same(model, updateCommand.Payload);
        Assert.Equal(scheduledTime, updateCommand.Payload.ScheduledPublishTimeUtc);
        Assert.Equal(ActivityEvents.PostUpdated, _commandMediator.Single<CreateActivityLogCommand>().EventKey);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SavePostCommand_AuditDispatchFailureOrCancellation_PreservesSaveAndLogsOnlySafeContext(bool canceled, bool includeTrace)
    {
        var logger = new Mock<ILogger<SavePostCommandHandler>>();
        var handler = CreateSavePostHandler(logger: logger.Object);
        var model = CreatePostEditModel(PostStatus.Draft);
        model.Title = "B2-PROTECTED-TITLE-SENTINEL";
        var postId = Guid.NewGuid();
        var savedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        _commandMediator.SetResult<CreatePostCommand, PostCommandResult>(new PostCommandResult
        {
            Id = postId,
            RouteLink = "captured-route",
            PostContent = model.EditorContent,
            LastModifiedUtc = savedAt
        });
        Exception failure = canceled
            ? new OperationCanceledException("B2-PROTECTED-EXCEPTION-SENTINEL")
            : new InvalidOperationException("B2-PROTECTED-EXCEPTION-SENTINEL");
        CreateActivityLogCommand? attempted = null;
        _commandMediator.BeforeSend = command =>
        {
            if (command is CreateActivityLogCommand activity)
            {
                attempted = activity;
                throw failure;
            }
        };
        const string trace = "b2-post-save-trace";
        var context = CreateContext() with { TraceIdentifier = includeTrace ? trace : null };
        var result = await handler.HandleAsync(new SavePostCommand(model, null, context), TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded);
        Assert.Equal(postId, result.PostId);
        Assert.Equal(savedAt, result.LastModifiedUtc);
        Assert.Equal(string.Empty, result.ErrorMessage);
        Assert.Same(model, _commandMediator.Single<CreatePostCommand>().Payload);
        Assert.NotNull(attempted);
        Assert.Equal("post.created", attempted.EventKey);
        Assert.Equal(postId.ToString(), attempted.TargetId);
        Assert.Equal(model.Title, attempted.TargetDisplayName);
        Assert.Equal(includeTrace ? trace : null, attempted.TraceIdentifier);
        var serializedCommand = JsonSerializer.Serialize(attempted, JsonSerializerOptions.Web);
        Assert.DoesNotContain("traceIdentifier", serializedCommand);
        Assert.DoesNotContain(trace, serializedCommand);
        var error = Assert.Single(logger.Invocations, invocation => Equals(invocation.Arguments[0], LogLevel.Error));
        Assert.Null(error.Arguments[3]);
        var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(error.Arguments[2]);
        Assert.Equal(includeTrace ? ["EventKey", "FailureType", "TraceIdentifier", "{OriginalFormat}"] :
            new[] { "EventKey", "FailureType", "{OriginalFormat}" }, state.Select(item => item.Key).ToArray());
        Assert.Contains(state, item => item.Key == "EventKey" && Equals(item.Value, "post.created"));
        Assert.Contains(state, item => item.Key == "FailureType" && Equals(item.Value, failure.GetType().Name));
        if (includeTrace)
            Assert.Contains(state, item => item.Key == "TraceIdentifier" && Equals(item.Value, trace));
        else
            Assert.Contains(state, item => item.Key == "{OriginalFormat}" &&
                Equals(item.Value, "Post was saved, but activity logging failed for event {EventKey}. Failure type: {FailureType}."));
        Assert.DoesNotContain(model.Title, error.Arguments[2]!.ToString());
        Assert.DoesNotContain(failure.Message, error.Arguments[2]!.ToString());
        Assert.DoesNotContain("127.0.0.1", error.Arguments[2]!.ToString());
        Assert.DoesNotContain("admin", error.Arguments[2]!.ToString());
        Assert.DoesNotContain("unit-test", error.Arguments[2]!.ToString());
        Assert.DoesNotContain("https://example.com", error.Arguments[2]!.ToString());
    }

    private SavePostCommandHandler CreateSavePostHandler(ScheduledPublishWakeUp? wakeUp = null,
        ILogger<SavePostCommandHandler>? logger = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IndexNow:MinimalIntervalMinutes"] = "10"
            })
            .Build();

        var blogConfig = new BlogConfig(new BlogSettingsSnapshot
        {
            AdvancedSettings = new AdvancedSettings
            {
                EnableWebmention = false
            }
        });

        return new SavePostCommandHandler(
            configuration,
            _commandMediator,
            blogConfig,
            wakeUp ?? new ScheduledPublishWakeUp(),
            logger ?? Mock.Of<ILogger<SavePostCommandHandler>>(),
            CreateCannonService(),
            TimeProvider.System);
    }

    private static CannonService CreateCannonService()
    {
        return new CannonService(
            Mock.Of<ILogger<CannonService>>(),
            Mock.Of<IServiceScopeFactory>());
    }

    private static PostOperationContext CreateContext()
    {
        return new(
            "admin",
            "127.0.0.1",
            "unit-test",
            "https://example.com");
    }

    private static PostEditModel CreatePostEditModel(PostStatus status)
    {
        return new()
        {
            Title = "Test Post",
            Slug = "test-post",
            Author = "Tester",
            SelectedCatIds = [],
            EnableComment = true,
            EditorContent = "<p>Hello</p>",
            PostStatus = status,
            ContentType = "html",
            Featured = false,
            FeedIncluded = true,
            Tags = "test",
            LanguageCode = "en-US",
            Abstract = "Hello",
            Keywords = "test"
        };
    }

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        private readonly Dictionary<Type, object> _results = [];

        public List<ICommand> Commands { get; } = [];
        public Action<ICommand>? BeforeSend { get; set; }

        public Task<MediationResult> TrySendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationResult<TCommandResult>> TrySendAsync<TCommandResult>(ICommand<TCommandResult> command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationDecision> EvaluateAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken)
        {
            BeforeSend?.Invoke(command);
            Commands.Add(command);
            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings? settings,
            CancellationToken cancellationToken)
        {
            BeforeSend?.Invoke(command);
            Commands.Add(command);
            return Task.FromResult((TCommandResult)_results[command.GetType()]!);
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
