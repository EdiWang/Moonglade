using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Web.Controllers;
using Moq;
using System.Net;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class CategoryControllerTests
{
    private readonly Mock<IQueryMediator> _queryMediator = new();
    private readonly RecordingCommandMediator _commandMediator = new();

    [Fact]
    public async Task Get_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetCategoryQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryEntity)null);

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
        var id = Guid.NewGuid();
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
                Id = Guid.NewGuid(),
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
        Assert.Equal(EventType.CategoryCreated, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Create Category", activityCommand.Operation);
        Assert.Equal(command.DisplayName, activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(command.Slug, activityCommand.MetaData!.GetType().GetProperty(nameof(command.Slug))!.GetValue(activityCommand.MetaData));
        Assert.Equal(command.Note, activityCommand.MetaData.GetType().GetProperty(nameof(command.Note))!.GetValue(activityCommand.MetaData));
    }

    [Fact]
    public async Task Update_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
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
        var id = Guid.NewGuid();
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
        Assert.Equal(EventType.CategoryUpdated, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Update Category", activityCommand.Operation);
        Assert.Equal(command.DisplayName, activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(id, activityCommand.MetaData!.GetType().GetProperty(nameof(command.Id))!.GetValue(activityCommand.MetaData));
        Assert.Equal(command.Slug, activityCommand.MetaData.GetType().GetProperty(nameof(command.Slug))!.GetValue(activityCommand.MetaData));
        Assert.Equal(command.Note, activityCommand.MetaData.GetType().GetProperty(nameof(command.Note))!.GetValue(activityCommand.MetaData));
    }

    [Fact]
    public async Task Delete_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
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
        var id = Guid.NewGuid();
        _commandMediator.SetResult<DeleteCategoryCommand, OperationCode>(OperationCode.Done);
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, _commandMediator.Single<DeleteCategoryCommand>().Id);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.CategoryDeleted, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Delete Category", activityCommand.Operation);
        Assert.Equal(id.ToString(), activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal(id, activityCommand.MetaData!.GetType().GetProperty("CategoryId")!.GetValue(activityCommand.MetaData));
    }

    private CategoryController CreateController(
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var controller = new CategoryController(_queryMediator.Object, _commandMediator);
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
