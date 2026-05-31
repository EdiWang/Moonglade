using Edi.CacheAside.InMemory;
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

namespace Moonglade.Web.Tests;

public class PostManagementCommandTests
{
    private const string PostCachePartition = "Post";

    private readonly Mock<ICacheAside> _cacheMock = new();
    private readonly RecordingCommandMediator _commandMediator = new();

    [Fact]
    public async Task SavePostCommand_ScheduledWithoutClientTimeZone_ReturnsConflictAndSkipsSave()
    {
        var handler = CreateSavePostHandler();
        var model = CreatePostEditModel(PostStatus.Scheduled);
        model.ScheduledPublishTime = DateTime.UtcNow.AddHours(1);
        model.ClientTimeZoneId = string.Empty;

        var result = await handler.HandleAsync(new SavePostCommand(model, CreateContext()), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Client time zone ID is required for scheduled posts.", result.ErrorMessage);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task SavePostCommand_CreatePost_RemovesPostCacheAndWritesActivityLog()
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

        var result = await handler.HandleAsync(new SavePostCommand(model, CreateContext()), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(postId, result.PostId);
        _cacheMock.Verify(c => c.Remove(PostCachePartition, routeLink), Times.Once);
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
        model.ScheduledPublishTime = scheduledTime;
        model.ClientTimeZoneId = "UTC";

        _commandMediator.SetResult<UpdatePostCommand, PostCommandResult>(new PostCommandResult
        {
            Id = postId,
            PostContent = model.EditorContent,
            LastModifiedUtc = DateTime.UtcNow
        });

        var result = await handler.HandleAsync(new SavePostCommand(model, CreateContext()), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(wakeToken.IsCancellationRequested);
        var updateCommand = _commandMediator.Single<UpdatePostCommand>();
        Assert.Equal(postId, updateCommand.Id);
        Assert.Same(model, updateCommand.Payload);
        Assert.Equal(scheduledTime, updateCommand.Payload.ScheduledPublishTime);
        Assert.Equal(EventType.PostUpdated, _commandMediator.Single<CreateActivityLogCommand>().EventType);
    }

    [Fact]
    public async Task PublishPostWorkflowCommand_PublishesPostClearsCacheAndWritesActivityLog()
    {
        var postId = Guid.NewGuid();
        var handler = new PublishPostWorkflowCommandHandler(_cacheMock.Object, _commandMediator);

        await handler.HandleAsync(new PublishPostWorkflowCommand(postId, CreateContext()), TestContext.Current.CancellationToken);

        Assert.Equal(postId, _commandMediator.Single<PublishPostCommand>().Id);
        _cacheMock.Verify(c => c.Remove(PostCachePartition, postId.ToString()), Times.Once);
        Assert.Equal(EventType.PostPublished, _commandMediator.Single<CreateActivityLogCommand>().EventType);
    }

    [Fact]
    public async Task UnpublishPostWorkflowCommand_UnpublishesPostClearsCacheAndWritesActivityLog()
    {
        var postId = Guid.NewGuid();
        var handler = new UnpublishPostWorkflowCommandHandler(_cacheMock.Object, _commandMediator);

        await handler.HandleAsync(new UnpublishPostWorkflowCommand(postId, CreateContext()), TestContext.Current.CancellationToken);

        Assert.Equal(postId, _commandMediator.Single<UnpublishPostCommand>().Id);
        _cacheMock.Verify(c => c.Remove(PostCachePartition, postId.ToString()), Times.Once);
        Assert.Equal(EventType.PostUnpublished, _commandMediator.Single<CreateActivityLogCommand>().EventType);
    }

    [Fact]
    public async Task RecyclePostCommand_MovesPostToRecycleBinAndWritesActivityLog()
    {
        var postId = Guid.NewGuid();
        var handler = new RecyclePostCommandHandler(_commandMediator);

        await handler.HandleAsync(new RecyclePostCommand(postId, CreateContext()), TestContext.Current.CancellationToken);

        var deleteCommand = _commandMediator.Single<DeletePostCommand>();
        Assert.Equal(postId, deleteCommand.Id);
        Assert.True(deleteCommand.SoftDelete);
        Assert.Equal(EventType.PostDeleted, _commandMediator.Single<CreateActivityLogCommand>().EventType);
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
            _cacheMock.Object,
            configuration,
            _commandMediator,
            blogConfig,
            wakeUp ?? new ScheduledPublishWakeUp(),
            Mock.Of<ILogger<SavePostCommandHandler>>(),
            CreateCannonService());
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