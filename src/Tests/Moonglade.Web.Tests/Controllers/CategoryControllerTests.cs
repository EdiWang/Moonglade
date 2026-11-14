using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public class CategoryControllerTests
{
    private readonly Mock<IQueryMediator> _queryMediator = new();
    private readonly RecordingCommandMediator _commandMediator = new();

    [Fact]
    public void ArticleEditJson_IntegerCategoryIds_PreservesPostGuidAndRejectsLegacyGuidCategoryIds()
    {
        var postId = Guid.NewGuid();
        var json = $$"""{"postId":"{{postId}}","selectedCatIds":[100,101]}""";
        var model = JsonSerializer.Deserialize<Moonglade.Web.Models.PostEditRequest>(json, JsonSerializerOptions.Web)!;
        Assert.Equal(postId, model.PostId);
        Assert.Equal([100, 101], model.SelectedCatIds);
        var detail = new Moonglade.Data.DTO.PostEditDetail { PostId = postId, SelectedCatIds = model.SelectedCatIds };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(detail, JsonSerializerOptions.Web));
        Assert.Equal(postId, document.RootElement.GetProperty("postId").GetGuid());
        Assert.Equal([100, 101], document.RootElement.GetProperty("selectedCatIds")
            .EnumerateArray().Select(value => value.GetInt32()).ToArray());
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Moonglade.Web.Models.PostEditRequest>(
            $$"""{"selectedCatIds":["{{Guid.NewGuid()}}"]}""", JsonSerializerOptions.Web));
    }

    [Theory]
    [InlineData("GET", 1)]
    [InlineData("GET", 100)]
    [InlineData("GET", int.MaxValue)]
    [InlineData("PUT", 100)]
    [InlineData("PUT", 1)]
    [InlineData("PUT", int.MaxValue)]
    [InlineData("DELETE", 100)]
    [InlineData("DELETE", 1)]
    [InlineData("DELETE", int.MaxValue)]
    public async Task CategoryHttpRoutes_PositiveInteger_DispatchesExactId(string method, int id)
    {
        _queryMediator.Setup(x => x.QueryAsync(It.IsAny<GetCategoryQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CategoryEntity { Id = id, Slug = "cloud", DisplayName = "Cloud", Note = "Cloud" });
        _commandMediator.SetResult<UpdateCategoryCommand, OperationCode>(OperationCode.Done);
        _commandMediator.SetResult<DeleteCategoryCommand, OperationCode>(OperationCode.Done);
        await using var app = await CreateHttpApp();
        using var client = app.GetTestClient();
        using var request = CategoryRequest(method, id.ToString());
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(method == "GET" ? HttpStatusCode.OK : HttpStatusCode.NoContent, response.StatusCode);
        if (method == "GET")
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(id, document.RootElement.GetProperty("id").GetInt32());
            _queryMediator.Verify(x => x.QueryAsync(It.Is<GetCategoryQuery>(query => query.Id == id),
                It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        else if (method == "PUT")
            Assert.Equal(id, _commandMediator.Single<UpdateCategoryCommand>().Id);
        else
            Assert.Equal(id, _commandMediator.Single<DeleteCategoryCommand>().Id);
    }

    [Theory]
    [InlineData("GET", "0", HttpStatusCode.BadRequest)]
    [InlineData("GET", "-1", HttpStatusCode.BadRequest)]
    [InlineData("PUT", "-1", HttpStatusCode.BadRequest)]
    [InlineData("PUT", "0", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", "0", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", "-1", HttpStatusCode.BadRequest)]
    [InlineData("GET", "a9d3e4f8-4321-4567-89ab-0123456789ab", HttpStatusCode.NotFound)]
    [InlineData("PUT", "a9d3e4f8-4321-4567-89ab-0123456789ab", HttpStatusCode.NotFound)]
    [InlineData("DELETE", "a9d3e4f8-4321-4567-89ab-0123456789ab", HttpStatusCode.NotFound)]
    [InlineData("GET", "2147483648", HttpStatusCode.NotFound)]
    public async Task CategoryHttpRoutes_InvalidOrLegacyId_DoesNotDispatch(string method, string id, HttpStatusCode status)
    {
        await using var app = await CreateHttpApp();
        using var client = app.GetTestClient();
        using var request = CategoryRequest(method, id);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(status, response.StatusCode);
        Assert.Empty(_commandMediator.Commands);
        _queryMediator.Verify(x => x.QueryAsync(It.IsAny<GetCategoryQuery>(), It.IsAny<QueryMediationSettings>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<WebApplication> CreateHttpApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(_queryMediator.Object);
        builder.Services.AddSingleton<ICommandMediator>(_commandMediator);
        builder.Services.AddControllers().AddApplicationPart(typeof(CategoryController).Assembly)
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new CategoryOnlyFeatureProvider()));
        builder.Services.AddAuthorization(options => options.AddPolicy(BlogAuthSchemas.AdministratorPolicy,
            policy => policy.RequireAssertion(_ => true)));
        var app = builder.Build();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static HttpRequestMessage CategoryRequest(string method, string id) =>
        new(new HttpMethod(method), $"/api/category/{id}")
        {
            Content = method == "PUT" ? JsonContent.Create(new UpdateCategoryCommand
            {
                DisplayName = "Cloud",
                Slug = "cloud",
                Note = "Cloud"
            }) : null
        };

    private sealed class CategoryOnlyFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.Where(type => type.AsType() != typeof(CategoryController)).ToArray())
                feature.Controllers.Remove(controller);
        }
    }

    [Fact]
    public async Task Get_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var id = 999;
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetCategoryQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryEntity)null!);

        var controller = CreateController();

        var result = await controller.Get(id);

        Assert.IsType<NotFoundResult>(result);
        _queryMediator.Verify(
            x => x.QueryAsync(
                It.Is<GetCategoryQuery>(query => query.Id == id),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Get_WhenCategoryExists_ReturnsOkWithCategory()
    {
        var id = 100;
        var category = new CategoryEntity
        {
            Id = id,
            Slug = "dotnet",
            DisplayName = ".NET",
            Note = ".NET posts"
        };

        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetCategoryQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var controller = CreateController();

        var result = await controller.Get(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(category, okResult.Value);
    }

    [Fact]
    public async Task List_ReturnsOkWithCategories()
    {
        var categories = new List<CategoryEntity>
        {
            new()
            {
                Id = 100,
                Slug = "dotnet",
                DisplayName = ".NET",
                Note = ".NET posts"
            }
        };

        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<ListCategoriesQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var controller = CreateController();

        var result = await controller.List();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(categories, okResult.Value);
    }

    [Fact]
    public async Task Create_SendsCommandReturnsCreatedAndWritesActivityLog()
    {
        var command = new CreateCategoryCommand
        {
            DisplayName = ".NET",
            Slug = "dotnet",
            Note = ".NET posts"
        };
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Create(command);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(string.Empty, createdResult.Location);
        Assert.Same(command, createdResult.Value);
        Assert.Same(command, _commandMediator.Single<CreateCategoryCommand>());

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.CategoryCreated, activityCommand.EventKey);
        Assert.Equal("admin", activityCommand.ActorDisplayName);
        Assert.Equal(command.DisplayName, activityCommand.TargetDisplayName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.Details);
        Assert.Equal(command.Slug, ActivityLogMetaDataAssert.Value<string>(activityCommand, nameof(command.Slug)));
        Assert.Null(activityCommand.TargetId);
        Assert.Equal(["Slug"], activityCommand.Details.Keys.ToArray());
    }

    [Fact]
    public async Task Update_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var id = 999;
        var command = new UpdateCategoryCommand
        {
            DisplayName = ".NET",
            Slug = "dotnet",
            Note = ".NET posts"
        };
        _commandMediator.SetResult<UpdateCategoryCommand, OperationCode>(OperationCode.ObjectNotFound);
        var controller = CreateController();

        var result = await controller.Update(id, command);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(id, command.Id);
        Assert.Same(command, _commandMediator.Single<UpdateCategoryCommand>());
        Assert.Empty(_commandMediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Update_WhenCategoryExists_ReturnsNoContentAndWritesActivityLog()
    {
        var id = 100;
        var command = new UpdateCategoryCommand
        {
            DisplayName = ".NET",
            Slug = "dotnet",
            Note = ".NET posts"
        };
        _commandMediator.SetResult<UpdateCategoryCommand, OperationCode>(OperationCode.Done);
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Update(id, command);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, command.Id);
        Assert.Same(command, _commandMediator.Single<UpdateCategoryCommand>());

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.CategoryUpdated, activityCommand.EventKey);
        Assert.Equal("admin", activityCommand.ActorDisplayName);
        Assert.Equal(command.DisplayName, activityCommand.TargetDisplayName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.Details);
        Assert.Equal(id, ActivityLogMetaDataAssert.Value<int>(activityCommand, nameof(command.Id)));
        Assert.Equal(command.Slug, ActivityLogMetaDataAssert.Value<string>(activityCommand, nameof(command.Slug)));
        Assert.Equal(id.ToString(), activityCommand.TargetId);
        Assert.Equal(["Id", "Slug"], activityCommand.Details.Keys.Order().ToArray());
    }

    [Fact]
    public async Task Delete_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var id = 999;
        _commandMediator.SetResult<DeleteCategoryCommand, OperationCode>(OperationCode.ObjectNotFound);
        var controller = CreateController();

        var result = await controller.Delete(id);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(id, _commandMediator.Single<DeleteCategoryCommand>().Id);
        Assert.Empty(_commandMediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Delete_WhenCategoryExists_ReturnsNoContentAndWritesActivityLog()
    {
        var id = 100;
        _commandMediator.SetResult<DeleteCategoryCommand, OperationCode>(OperationCode.Done);
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, _commandMediator.Single<DeleteCategoryCommand>().Id);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.CategoryDeleted, activityCommand.EventKey);
        Assert.Equal("admin", activityCommand.ActorDisplayName);
        Assert.Equal(id.ToString(), activityCommand.TargetId);
        Assert.Equal(id.ToString(), activityCommand.TargetDisplayName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.Details);
        Assert.Equal(id, ActivityLogMetaDataAssert.Value<int>(activityCommand, "CategoryId"));
    }

    private CategoryController CreateController(
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var controller = new CategoryController(_queryMediator.Object, _commandMediator,
            Moq.Mock.Of<Microsoft.Extensions.Logging.ILogger<CategoryController>>());
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
