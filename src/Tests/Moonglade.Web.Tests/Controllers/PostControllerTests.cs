using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
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

namespace Moonglade.Web.Tests.Controllers;

public class PostControllerTests
{
    [Theory]
    [InlineData("recycle", ActivityEvents.PostDeleted)]
    [InlineData("publish", ActivityEvents.PostPublished)]
    [InlineData("unpublish", ActivityEvents.PostUnpublished)]
    public async Task StateChange_DispatchesFeatureCommandThenActivityLog(
        string operation, string eventKey)
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
        Assert.Equal(eventKey, activity.EventKey);
        Assert.Equal($"Post #{postId}", activity.TargetDisplayName);
        Assert.Equal(postId.ToString(), activity.TargetId);
        Assert.Equal(ActivityActorKind.Administrator, activity.ActorKind);
        Assert.Equal("admin", activity.ActorDisplayName);
        Assert.Equal("192.0.2.1", activity.IpAddress);
        Assert.Equal("post-controller-test", activity.UserAgent);
        var metadata = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(activity.Details);
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
    public async Task StateChange_WhenActivityDispatchFails_PreservesSuccessfulBusinessResponse()
    {
        var commandMediator = new Mock<ICommandMediator>();
        commandMediator.Setup(mediator => mediator.SendAsync(
                It.IsAny<ICommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var failure = new InvalidOperationException("Audit unavailable.");
        commandMediator.Setup(mediator => mediator.SendAsync(
                It.IsAny<CreateActivityLogCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var controller = CreateStateChangeController(commandMediator.Object);
        var postId = Guid.NewGuid();

        Assert.IsType<NoContentResult>(await controller.Publish(postId));

        commandMediator.Verify(mediator => mediator.SendAsync(
            It.Is<PublishPostCommand>(command => command.Id == postId), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMeta_IntegerCategoryIds_SerializesExactNumericIdsAndNames()
    {
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(mediator => mediator.QueryAsync(It.IsAny<ListCategoriesQuery>(),
                It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CategoryEntity { Id = 100, DisplayName = "Cloud", Slug = "cloud", Note = "Cloud" },
                new CategoryEntity { Id = 101, DisplayName = "Cloud", Slug = "other", Note = "Other" }
            ]);
        var controller = new PostController(Mock.Of<ICommandMediator>(), queryMediator.Object,
            new BlogConfig(), Mock.Of<IStringLocalizer<Program>>(), Mock.Of<ILogger<PostController>>());

        var result = await controller.GetMeta(Options.Create(new RequestLocalizationOptions()), new Moonglade.Web.Tests.TestSupport.TestOwnerProfileReader());
        var metadata = Assert.IsType<PostEditorMeta>(Assert.IsType<OkObjectResult>(result).Value);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(metadata, JsonSerializerOptions.Web));
        var categories = document.RootElement.GetProperty("categories").EnumerateArray().ToArray();
        Assert.Equal([100, 101], categories.Select(category => category.GetProperty("id").GetInt32()).ToArray());
        Assert.All(categories, category => Assert.Equal(JsonValueKind.Number, category.GetProperty("id").ValueKind));
        Assert.Equal(["Cloud", "Cloud"], categories.Select(category => category.GetProperty("displayName").GetString()).ToArray());
    }

    [Fact]
    public async Task GetPost_IntegerCategoryLinks_SerializesSelectionWithoutChangingPostGuid()
    {
        var postId = Guid.NewGuid();
        var post = new PostEntity { Id = postId, Title = "Categorized article", Slug = "article", PostContent = "Content" };
        post.PostCategory.Add(new PostCategoryEntity { PostId = postId, CategoryId = 101 });
        post.PostCategory.Add(new PostCategoryEntity { PostId = postId, CategoryId = 100 });
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(mediator => mediator.QueryAsync(It.Is<GetPostByIdQuery>(query => query.Id == postId),
            It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(post);
        var controller = new PostController(Mock.Of<ICommandMediator>(), queryMediator.Object,
            new BlogConfig(), Mock.Of<IStringLocalizer<Program>>(), Mock.Of<ILogger<PostController>>());

        var result = await controller.GetPost(postId);
        var detail = Assert.IsType<PostEditDetail>(Assert.IsType<OkObjectResult>(result).Value);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(detail, JsonSerializerOptions.Web));
        Assert.Equal(postId, document.RootElement.GetProperty("postId").GetGuid());
        Assert.Equal([100, 101], document.RootElement.GetProperty("selectedCatIds")
            .EnumerateArray().Select(id => id.GetInt32()).Order().ToArray());
        Assert.Equal("Categorized article", document.RootElement.GetProperty("title").GetString());
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
            Mock.Of<IStringLocalizer<Program>>(), Mock.Of<ILogger<PostController>>());
        var localizationOptions = Options.Create(new RequestLocalizationOptions());

        var initialResult = await controller.GetMeta(localizationOptions, new Moonglade.Web.Tests.TestSupport.TestOwnerProfileReader());
        var initialMeta = Assert.IsType<PostEditorMeta>(Assert.IsType<OkObjectResult>(initialResult).Value);
        Assert.Equal("html", initialMeta.EditorChoice);

        blogConfig.Publish(blogConfig.Capture() with { ContentSettings = new ContentSettings { DefaultEditor = EditorChoice.Markdown } });

        var updatedResult = await controller.GetMeta(localizationOptions, new Moonglade.Web.Tests.TestSupport.TestOwnerProfileReader());
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
            Mock.Of<IStringLocalizer<Program>>(), Mock.Of<ILogger<PostController>>());

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
            localizer.Object, Mock.Of<ILogger<PostController>>())
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
            Mock.Of<IStringLocalizer<Program>>(), Mock.Of<ILogger<PostController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("example.com");
        controller.HttpContext.TraceIdentifier = "b2-http-post-trace";

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
        Assert.Equal("b2-http-post-trace", dispatchedCommand.Context.TraceIdentifier);
        var serializedContext = JsonSerializer.Serialize(dispatchedCommand.Context, JsonSerializerOptions.Web);
        Assert.DoesNotContain("traceIdentifier", serializedContext);
        Assert.DoesNotContain("b2-http-post-trace", serializedContext);
    }
    private static PostController CreateStateChangeController(ICommandMediator commandMediator)
    {
        var controller = new PostController(
            commandMediator,
            Mock.Of<IQueryMediator>(),
            new BlogConfig(),
            Mock.Of<IStringLocalizer<Program>>(), Mock.Of<ILogger<PostController>>())
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
