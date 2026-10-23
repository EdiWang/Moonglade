using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moonglade.ActivityLog;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moonglade.Web.Commands;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests;

public class PostControllerTests
{
    [Theory]
    [InlineData("recycle", EventType.PostDeleted, "Delete Post (Move to Recycle Bin)")]
    [InlineData("publish", EventType.PostPublished, "Publish Post")]
    [InlineData("unpublish", EventType.PostUnpublished, "Unpublish Post")]
    public async Task StateChange_DispatchesFeatureCommandThenActivityLog(
        string operation, EventType eventType, string logOperation)
    {
        var commands = new List<ICommand>();
        var commandMediator = new Mock<ICommandMediator>();
        commandMediator
            .Setup(mediator => mediator.SendAsync(
                It.IsAny<ICommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Callback<ICommand, CommandMediationSettings, CancellationToken>((command, _, _) => commands.Add(command))
            .Returns(Task.CompletedTask);
        var controller = CreateStateChangeController(commandMediator.Object);
        var postId = Guid.NewGuid();

        var result = await InvokeStateChangeAsync(controller, operation, postId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(2, commands.Count);
        ICommand expectedCommand = operation switch
        {
            "recycle" => new DeletePostCommand(postId, SoftDelete: true),
            "publish" => new PublishPostCommand(postId),
            "unpublish" => new UnpublishPostCommand(postId),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        Assert.Equal(expectedCommand, commands[0]);
        var activity = Assert.IsType<CreateActivityLogCommand>(commands[1]);
        Assert.Equal(eventType, activity.EventType);
        Assert.Equal(logOperation, activity.Operation);
        Assert.Equal($"Post #{postId}", activity.TargetName);
        Assert.Equal("admin", activity.ActorId);
        Assert.Equal("192.0.2.1", activity.IpAddress);
        Assert.Equal("post-controller-test", activity.UserAgent);
        var metadata = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(activity.MetaData);
        Assert.Equal(postId, Assert.Single(metadata).Value);
        Assert.True(metadata.ContainsKey("PostId"));
    }

    [Theory]
    [InlineData("recycle")]
    [InlineData("publish")]
    [InlineData("unpublish")]
    public async Task StateChange_FeatureCommandFails_DoesNotWriteSuccessActivity(string operation)
    {
        var commandMediator = new Mock<ICommandMediator>();
        var failure = new InvalidOperationException("Write failed.");
        commandMediator
            .Setup(mediator => mediator.SendAsync(
                It.IsAny<ICommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var controller = CreateStateChangeController(commandMediator.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeStateChangeAsync(controller, operation, Guid.NewGuid()));

        Assert.Same(failure, exception);
        commandMediator.Verify(mediator => mediator.SendAsync(
            It.IsAny<CreateActivityLogCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMeta_UsesCurrentDefaultEditorWithoutRestart()
    {
        var blogConfig = new BlogConfig();
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator
            .Setup(mediator => mediator.QueryAsync(
                It.IsAny<ListCategoriesQuery>(),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var controller = new PostController(
            Mock.Of<ICommandMediator>(),
            queryMediator.Object,
            blogConfig,
            Mock.Of<IStringLocalizer<Program>>());
        var localizationOptions = Options.Create(new RequestLocalizationOptions());

        var initialResult = await controller.GetMeta(localizationOptions);
        var initialMeta = Assert.IsType<PostEditorMeta>(Assert.IsType<OkObjectResult>(initialResult).Value);
        Assert.Equal("html", initialMeta.EditorChoice);

        blogConfig.ContentSettings = new ContentSettings { DefaultEditor = EditorChoice.Markdown };

        var updatedResult = await controller.GetMeta(localizationOptions);
        var updatedMeta = Assert.IsType<PostEditorMeta>(Assert.IsType<OkObjectResult>(updatedResult).Value);
        Assert.Equal("markdown", updatedMeta.EditorChoice);
    }

    [Fact]
    public void PostEditRequest_NullLastModifiedUtc_DeserializesAsNoPreviousSave()
    {
        const string json = """{"lastModifiedUtc":null}""";

        var request = JsonSerializer.Deserialize<PostEditRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Null(request.LastModifiedUtc);
    }

    [Fact]
    public async Task GetPost_UtcTimestamps_SerializesExplicitUtcDesignators()
    {
        var postId = Guid.NewGuid();
        var publishDateUtc = new DateTime(2026, 8, 15, 1, 2, 3, DateTimeKind.Utc);
        var scheduledPublishTimeUtc = publishDateUtc.AddDays(1);
        var lastModifiedUtc = publishDateUtc.AddMinutes(5).AddTicks(1_234_567);
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator
            .Setup(mediator => mediator.QueryAsync(
                It.Is<GetPostByIdQuery>(query => query.Id == postId),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostEntity
            {
                Id = postId,
                Title = "UTC post",
                Slug = "utc-post",
                PostContent = "Content",
                ContentType = "markdown",
                PubDateUtc = publishDateUtc,
                ScheduledPublishTimeUtc = scheduledPublishTimeUtc,
                LastModifiedUtc = lastModifiedUtc
            });
        var controller = new PostController(
            Mock.Of<ICommandMediator>(),
            queryMediator.Object,
            new BlogConfig(),
            Mock.Of<IStringLocalizer<Program>>());

        var result = await controller.GetPost(postId);

        var response = Assert.IsType<PostEditDetail>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(lastModifiedUtc, response.LastModifiedUtc);
        Assert.Equal("markdown", response.ContentType);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"publishDate\":\"2026-08-15T01:02:03Z\"", json);
        Assert.Contains("\"scheduledPublishTimeUtc\":\"2026-08-16T01:02:03Z\"", json);
        Assert.Contains("\"lastModifiedUtc\":\"2026-08-15T01:07:03.1234567Z\"", json);
    }

    [Fact]
    public async Task CreateOrEdit_ScheduleValidationFailure_ReturnsLocalizedValidationProblem()
    {
        const string localizedMessage = "Choose another local time.";
        var commandMediator = new Mock<ICommandMediator>(MockBehavior.Strict);
        var localizer = new Mock<IStringLocalizer<Program>>();
        localizer
            .Setup(value => value[ScheduledPublishValidationMessages.InvalidLocalTime])
            .Returns(new LocalizedString(
                ScheduledPublishValidationMessages.InvalidLocalTime,
                localizedMessage));
        var controller = new PostController(
            commandMediator.Object,
            Mock.Of<IQueryMediator>(),
            new BlogConfig(),
            localizer.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("example.com");

        var result = await controller.CreateOrEdit(new PostEditRequest
        {
            PostStatus = PostStatus.Scheduled,
            ScheduledPublishLocalTime = new DateTime(2030, 3, 10, 2, 30, 0, DateTimeKind.Unspecified),
            ClientTimeZoneId = "America/New_York"
        });

        var problemResult = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(problemResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(localizedMessage, problem.Detail);
        Assert.Equal(
            [localizedMessage],
            problem.Errors[nameof(PostEditRequest.ScheduledPublishLocalTime)]);
    }

    [Fact]
    public async Task CreateOrEdit_ValidLocalSchedule_DispatchesUtcFeatureValue()
    {
        SavePostCommand? dispatchedCommand = null;
        var commandMediator = new Mock<ICommandMediator>();
        commandMediator
            .Setup(mediator => mediator.SendAsync(
                It.IsAny<SavePostCommand>(),
                It.IsAny<CommandMediationSettings>(),
                It.IsAny<CancellationToken>()))
            .Callback<ICommand<PostOperationResult>, CommandMediationSettings, CancellationToken>((command, _, _) =>
                dispatchedCommand = Assert.IsType<SavePostCommand>(command))
            .ReturnsAsync(PostOperationResult.Success(Guid.NewGuid(), null));
        var controller = new PostController(
            commandMediator.Object,
            Mock.Of<IQueryMediator>(),
            new BlogConfig(),
            Mock.Of<IStringLocalizer<Program>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("example.com");

        var result = await controller.CreateOrEdit(new PostEditRequest
        {
            PostStatus = PostStatus.Scheduled,
            ScheduledPublishLocalTime = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Unspecified),
            ClientTimeZoneId = "UTC"
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(dispatchedCommand);
        Assert.Equal(
            new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            dispatchedCommand.Payload.ScheduledPublishTimeUtc);
        Assert.Equal(DateTimeKind.Utc, dispatchedCommand.Payload.ScheduledPublishTimeUtc!.Value.Kind);
    }
    private static PostController CreateStateChangeController(ICommandMediator commandMediator)
    {
        var controller = new PostController(
            commandMediator,
            Mock.Of<IQueryMediator>(),
            new BlogConfig(),
            Mock.Of<IStringLocalizer<Program>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "admin")], "test"));
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
        controller.Request.Headers.UserAgent = "post-controller-test";
        return controller;
    }

    private static Task<IActionResult> InvokeStateChangeAsync(PostController controller, string operation, Guid postId) => operation switch
    {
        "recycle" => controller.Delete(postId),
        "publish" => controller.Publish(postId),
        "unpublish" => controller.Unpublish(postId),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
