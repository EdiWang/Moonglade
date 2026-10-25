using LiteBus.Commands.Abstractions;
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
        Assert.Equal(EventType.PostCreated, activityLogCommand.EventType);
        Assert.Equal("Create Post", activityLogCommand.Operation);
        Assert.Equal(model.Title, activityLogCommand.TargetName);
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
        Assert.Equal(EventType.PostUpdated, _commandMediator.Single<CreateActivityLogCommand>().EventType);
    }

    private SavePostCommandHandler CreateSavePostHandler(ScheduledPublishWakeUp? wakeUp = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IndexNow:MinimalIntervalMinutes"] = "10"
            })
            .Build();

        var blogConfig = new BlogConfig
        {
            AdvancedSettings = new AdvancedSettings
            {
                EnableWebmention = false
            }
        };

        return new SavePostCommandHandler(
            configuration,
            _commandMediator,
            blogConfig,
            wakeUp ?? new ScheduledPublishWakeUp(),
            Mock.Of<ILogger<SavePostCommandHandler>>(),
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
