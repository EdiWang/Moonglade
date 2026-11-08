using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Web.Handlers;
using Moq;

namespace Moonglade.Web.Tests.Handlers;

public class ScheduledPostsPublishedEventHandlerTests
{
    private static readonly DateTime Occurrence = new(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private const string CapturedTitle = "<script>B4-CAPTURED-TITLE</script>";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task HandleAsync_Batch_EmitsOrderedIndependentSystemCommandsWithPublicationSnapshots(int count)
    {
        var posts = Publications(count);
        var commands = new Mock<ICommandMediator>();
        var captured = new List<CreateActivityLogCommand>();
        commands.Setup(m => m.SendAsync(It.IsAny<CreateActivityLogCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Callback((ICommand request, CommandMediationSettings _, CancellationToken token) =>
            {
                var command = Assert.IsType<CreateActivityLogCommand>(request);
                Assert.Equal(CancellationToken.None, token);
                captured.Add(command);
            }).Returns(Task.CompletedTask);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await new ScheduledPostsPublishedEventHandler(commands.Object, Mock.Of<ILogger<ScheduledPostsPublishedEventHandler>>())
            .HandleAsync(new(posts), cancellation.Token);

        Assert.Equal(count, captured.Count);
        Assert.Equal(posts.Select(post => post.PostId.ToString()), captured.Select(command => command.TargetId));
        for (var index = 0; index < count; index++)
        {
            var command = captured[index];
            Assert.Equal("post.automatically-published", command.EventKey);
            Assert.Equal(ActivityActorKind.System, command.ActorKind);
            Assert.Null(command.ActorDisplayName);
            Assert.Null(command.IpAddress);
            Assert.Null(command.UserAgent);
            Assert.Null(command.TraceIdentifier);
            Assert.Equal(CapturedTitle, command.TargetDisplayName);
            Assert.Equal(Occurrence.AddSeconds(index), command.EventTimeUtc);
            var detail = Assert.Single(command.Details!);
            Assert.Equal("PostId", detail.Key);
            Assert.Equal(posts[index].PostId, Assert.IsType<Guid>(detail.Value));
        }
        Assert.All(commands.Invocations, invocation => Assert.IsType<CreateActivityLogCommand>(invocation.Arguments[0]));
    }

    [Theory]
    [InlineData("dispatch-failure")]
    [InlineData("dispatch-cancellation")]
    public async Task HandleAsync_OneDispatchFailsOrCancels_ContinuesWithSafeDiagnostics(string scenario)
    {
        var posts = Publications(3);
        var sent = new List<string?>();
        var commands = new Mock<ICommandMediator>();
        const string exceptionContent = "B4-PRIVATE-BODY-AND-EXCEPTION-CONTENT";
        Exception failure = scenario == "dispatch-failure"
            ? new InvalidOperationException(exceptionContent)
            : new OperationCanceledException(exceptionContent);
        commands.Setup(m => m.SendAsync(It.IsAny<CreateActivityLogCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((CreateActivityLogCommand command, CommandMediationSettings _, CancellationToken token) =>
            {
                Assert.Equal(CancellationToken.None, token);
                sent.Add(command.TargetId);
                return sent.Count == 2 ? Task.FromException(failure) : Task.CompletedTask;
            });
        var logger = new Mock<ILogger<ScheduledPostsPublishedEventHandler>>();

        await new ScheduledPostsPublishedEventHandler(commands.Object, logger.Object)
            .HandleAsync(new(posts), TestContext.Current.CancellationToken);

        Assert.Equal(posts.Select(post => post.PostId.ToString()), sent);
        var diagnostic = Assert.Single(logger.Invocations, invocation => invocation.Method.Name == "Log");
        Assert.Equal(LogLevel.Error, diagnostic.Arguments[0]);
        Assert.Null(diagnostic.Arguments[3]);
        var fields = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(diagnostic.Arguments[2]);
        Assert.Equal(["EventKey", "FailureType", "{OriginalFormat}"], fields.Select(field => field.Key).ToArray());
        Assert.Contains(fields, field => field.Key == "{OriginalFormat}" &&
            Equals(field.Value, "Scheduled publication activity logging failed for {EventKey}. Failure type: {FailureType}"));
        var text = diagnostic.Arguments[2]!.ToString()!;
        Assert.Contains("post.automatically-published", text);
        Assert.Contains(failure.GetType().Name, text);
        Assert.DoesNotContain(exceptionContent, text);
        Assert.DoesNotContain(CapturedTitle, text);
        Assert.All(posts, post => Assert.DoesNotContain(post.PostId.ToString(), text));
    }

    [Fact]
    public async Task HandleAsync_PendingWrite_AwaitsItBeforeDispatchingNextPost()
    {
        var posts = Publications(2);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sent = new List<string?>();
        var commands = new Mock<ICommandMediator>();
        commands.Setup(m => m.SendAsync(It.IsAny<CreateActivityLogCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((CreateActivityLogCommand command, CommandMediationSettings _, CancellationToken token) =>
            {
                sent.Add(command.TargetId);
                if (sent.Count != 1) return Task.CompletedTask;
                started.SetResult();
                return finish.Task;
            });

        var pending = new ScheduledPostsPublishedEventHandler(commands.Object, Mock.Of<ILogger<ScheduledPostsPublishedEventHandler>>())
            .HandleAsync(new(posts), TestContext.Current.CancellationToken);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(posts[0].PostId.ToString(), Assert.Single(sent));
            Assert.False(pending.IsCompleted);
        }
        finally { finish.TrySetResult(); }
        await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(posts.Select(post => post.PostId.ToString()), sent);
    }

    private static ScheduledPostPublication[] Publications(int count) => Enumerable.Range(0, count)
        .Select(index => new ScheduledPostPublication(Guid.NewGuid(), CapturedTitle, Occurrence.AddSeconds(index))).ToArray();
}
