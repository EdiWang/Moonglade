using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Web.Commands;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moq;
using System.Text.Json;

namespace Moonglade.Web.Tests;

public class PostControllerTests
{
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
                PubDateUtc = publishDateUtc,
                ScheduledPublishTimeUtc = scheduledPublishTimeUtc,
                LastModifiedUtc = lastModifiedUtc
            });
        var controller = new PostController(
            new ConfigurationBuilder().Build(),
            Mock.Of<ICommandMediator>(),
            queryMediator.Object,
            new BlogConfig(),
            Mock.Of<IStringLocalizer<Program>>());

        var result = await controller.GetPost(postId);

        var response = Assert.IsType<PostEditDetail>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(lastModifiedUtc, response.LastModifiedUtc);

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
            new ConfigurationBuilder().Build(),
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
            new ConfigurationBuilder().Build(),
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
}
