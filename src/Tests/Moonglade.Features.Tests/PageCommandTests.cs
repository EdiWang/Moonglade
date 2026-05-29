using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Page;
using Moq;

namespace Moonglade.Features.Tests;

public class PageCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task CreatePageCommand_WithoutCss_DoesNotSendSaveStyleSheetCommand()
    {
        using var db = CreateDbContext();
        var mediator = new RecordingCommandMediator();
        var handler = new CreatePageCommandHandler(db, mediator, Mock.Of<ILogger<CreatePageCommandHandler>>());
        var request = CreateEditPageRequest();
        request.CssContent = string.Empty;
        var before = DateTime.UtcNow;

        var pageId = await handler.HandleAsync(new CreatePageCommand(request), TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        Assert.Empty(mediator.Commands);
        var page = await db.BlogPage.SingleAsync(p => p.Id == pageId, TestContext.Current.CancellationToken);
        Assert.Equal("test-page", page.Slug);
        Assert.Equal("Test Page", page.Title);
        Assert.Equal(string.Empty, page.CssId);
        Assert.InRange(page.CreateTimeUtc, before, after);
        Assert.InRange(page.UpdateTimeUtc!.Value, before, after);
    }

    [Fact]
    public async Task CreatePageCommand_WithCss_SendsSaveStyleSheetCommandAndStoresCssId()
    {
        using var db = CreateDbContext();
        var cssId = Guid.NewGuid();
        var mediator = new RecordingCommandMediator();
        mediator.SetResult<SaveStyleSheetCommand, Guid>(cssId);
        var handler = new CreatePageCommandHandler(db, mediator, Mock.Of<ILogger<CreatePageCommandHandler>>());
        var request = CreateEditPageRequest();
        request.Slug = "  Mixed-Slug  ";
        request.CssContent = "body { color: red; }";

        var pageId = await handler.HandleAsync(new CreatePageCommand(request), TestContext.Current.CancellationToken);

        var command = mediator.Single<SaveStyleSheetCommand>();
        Assert.Equal("mixed-slug", command.Slug);
        Assert.Equal(request.CssContent, command.CssContent);

        var page = await db.BlogPage.SingleAsync(p => p.Id == pageId, TestContext.Current.CancellationToken);
        Assert.Equal("mixed-slug", page.Slug);
        Assert.Equal(cssId.ToString(), page.CssId);
    }

    [Fact]
    public async Task UpdatePageCommand_UpdatesExistingPageFields()
    {
        using var db = CreateDbContext();
        var pageId = Guid.NewGuid();
        db.BlogPage.Add(CreatePageEntity(pageId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mediator = new RecordingCommandMediator();
        var handler = new UpdatePageCommandHandler(db, mediator, Mock.Of<ILogger<UpdatePageCommandHandler>>());
        var request = CreateEditPageRequest();
        request.Title = " Updated Page ";
        request.Slug = "  Updated-Slug  ";
        request.CssContent = string.Empty;
        var before = DateTime.UtcNow;

        var result = await handler.HandleAsync(new UpdatePageCommand(pageId, request), TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        Assert.Equal(pageId, result);
        Assert.Empty(mediator.Commands);
        var page = await db.BlogPage.SingleAsync(p => p.Id == pageId, TestContext.Current.CancellationToken);
        Assert.Equal("Updated Page", page.Title);
        Assert.Equal("updated-slug", page.Slug);
        Assert.Equal(request.MetaDescription, page.MetaDescription);
        Assert.Equal(request.RawHtmlContent, page.HtmlContent);
        Assert.Equal(request.HideSidebar, page.HideSidebar);
        Assert.Equal(request.IsPublished, page.IsPublished);
        Assert.Equal(string.Empty, page.CssId);
        Assert.InRange(page.UpdateTimeUtc!.Value, before, after);
    }

    [Fact]
    public async Task UpdatePageCommand_WithCss_SendsSaveStyleSheetCommand()
    {
        using var db = CreateDbContext();
        var pageId = Guid.NewGuid();
        db.BlogPage.Add(CreatePageEntity(pageId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cssId = Guid.NewGuid();
        var mediator = new RecordingCommandMediator();
        mediator.SetResult<SaveStyleSheetCommand, Guid>(cssId);
        var handler = new UpdatePageCommandHandler(db, mediator, Mock.Of<ILogger<UpdatePageCommandHandler>>());
        var request = CreateEditPageRequest();
        request.CssContent = "body { color: blue; }";

        await handler.HandleAsync(new UpdatePageCommand(pageId, request), TestContext.Current.CancellationToken);

        var command = mediator.Single<SaveStyleSheetCommand>();
        Assert.Equal(pageId, command.Id);
        Assert.Equal("test-page", command.Slug);
        Assert.Equal(request.CssContent, command.CssContent);

        var page = await db.BlogPage.SingleAsync(p => p.Id == pageId, TestContext.Current.CancellationToken);
        Assert.Equal(cssId.ToString(), page.CssId);
    }

    [Fact]
    public async Task UpdatePageCommand_PageDoesNotExist_ThrowsInvalidOperationException()
    {
        using var db = CreateDbContext();
        var handler = new UpdatePageCommandHandler(db, new RecordingCommandMediator(), Mock.Of<ILogger<UpdatePageCommandHandler>>());
        var pageId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new UpdatePageCommand(pageId, CreateEditPageRequest()), TestContext.Current.CancellationToken));

        Assert.Contains(pageId.ToString(), exception.Message);
    }

    [Fact]
    public async Task DeletePageCommand_SoftDelete_SetsIsDeleted()
    {
        using var db = CreateDbContext();
        var pageId = Guid.NewGuid();
        db.BlogPage.Add(CreatePageEntity(pageId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeletePageCommandHandler(db, new RecordingCommandMediator(), Mock.Of<ILogger<DeletePageCommandHandler>>());

        var result = await handler.HandleAsync(new DeletePageCommand(pageId, SoftDelete: true), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        var page = await db.BlogPage.SingleAsync(p => p.Id == pageId, TestContext.Current.CancellationToken);
        Assert.True(page.IsDeleted);
    }

    [Fact]
    public async Task DeletePageCommand_HardDelete_RemovesPageAndStyleSheet()
    {
        using var db = CreateDbContext();
        var pageId = Guid.NewGuid();
        var page = CreatePageEntity(pageId);
        db.BlogPage.Add(page);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mediator = new RecordingCommandMediator();
        var handler = new DeletePageCommandHandler(db, mediator, Mock.Of<ILogger<DeletePageCommandHandler>>());

        var result = await handler.HandleAsync(new DeletePageCommand(pageId), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        Assert.Empty(await db.BlogPage.ToListAsync(TestContext.Current.CancellationToken));
        var command = mediator.Single<DeleteStyleSheetCommand>();
        Assert.Equal(Guid.Parse(page.CssId), command.Id);
    }

    [Fact]
    public async Task RestorePageCommand_SetsIsDeletedFalse()
    {
        using var db = CreateDbContext();
        var pageId = Guid.NewGuid();
        var page = CreatePageEntity(pageId);
        page.IsDeleted = true;
        db.BlogPage.Add(page);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RestorePageCommandHandler(db, Mock.Of<ILogger<RestorePageCommandHandler>>());

        await handler.HandleAsync(new RestorePageCommand(pageId), TestContext.Current.CancellationToken);

        var restoredPage = await db.BlogPage.SingleAsync(p => p.Id == pageId, TestContext.Current.CancellationToken);
        Assert.False(restoredPage.IsDeleted);
    }

    private static EditPageRequest CreateEditPageRequest()
    {
        return new EditPageRequest
        {
            Title = "Test Page",
            Slug = "test-page",
            MetaDescription = "Meta description",
            RawHtmlContent = "<p>Hello</p>",
            CssContent = null,
            HideSidebar = false,
            IsPublished = true
        };
    }

    private static PageEntity CreatePageEntity(Guid id)
    {
        return new PageEntity
        {
            Id = id,
            Title = "Old Page",
            Slug = "old-page",
            MetaDescription = "Old meta",
            HtmlContent = "<p>Old</p>",
            CssId = Guid.NewGuid().ToString(),
            HideSidebar = true,
            IsPublished = false,
            CreateTimeUtc = DateTime.UtcNow.AddDays(-2),
            UpdateTimeUtc = DateTime.UtcNow.AddDays(-1)
        };
    }

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        private readonly Dictionary<Type, object> _results = [];

        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CommandMediationSettings settings, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings settings,
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
