using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moonglade.Configuration;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Web.Controllers;
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
            new BlogConfig());

        var result = await controller.GetPost(postId);

        var response = Assert.IsType<PostEditDetail>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("2026-08-15T01:07:03.1234567Z", response.LastModifiedUtc);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"publishDate\":\"2026-08-15T01:02:03Z\"", json);
        Assert.Contains("\"scheduledPublishTimeUtc\":\"2026-08-16T01:02:03Z\"", json);
        Assert.Contains("\"lastModifiedUtc\":\"2026-08-15T01:07:03.1234567Z\"", json);
    }
}
