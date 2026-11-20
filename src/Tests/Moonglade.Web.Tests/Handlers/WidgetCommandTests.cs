using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Widgets;

namespace Moonglade.Web.Tests.Handlers;

public class WidgetCommandTests
{
    [Fact]
    public async Task WidgetLifecycle_PreservesIdentityCreationTimeAndContentUntilDeletion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(ct);

        var create = new CreateWidgetCommandHandler(db, NullLogger<CreateWidgetCommandHandler>.Instance);
        var request = new EditWidgetRequest
        {
            Title = "  Fixture links  ",
            WidgetType = WidgetType.LinkList,
            ContentCode = "[{\"name\":\"Example\",\"url\":\"https://example.com\",\"order\":0}]",
            DisplayOrder = -30,
            IsEnabled = true
        };

        var id = await create.HandleAsync(new CreateWidgetCommand(request), ct);
        var created = await new GetWidgetQueryHandler(db).HandleAsync(new GetWidgetQuery(id), ct);

        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(created);
        Assert.Equal("Fixture links", created.Title);
        Assert.Equal(WidgetContentType.JSON, created.ContentType);
        Assert.Equal(request.ContentCode, created.ContentCode);
        Assert.Equal(DateTimeKind.Utc, created.CreatedTimeUtc.Kind);
        var createdTime = created.CreatedTimeUtc;

        var update = new UpdateWidgetCommandHandler(db, NullLogger<UpdateWidgetCommandHandler>.Instance);
        var updatedRequest = new EditWidgetRequest
        {
            Title = "  Updated links  ",
            WidgetType = WidgetType.LinkList,
            ContentCode = "[{\"name\":\"Changed\",\"url\":\"https://example.com/changed\",\"order\":1}]",
            DisplayOrder = 999,
            IsEnabled = false
        };

        Assert.Equal(OperationCode.Done,
            await update.HandleAsync(new UpdateWidgetCommand(id, updatedRequest), ct));
        var listed = Assert.Single(await new ListWidgetsQueryHandler(db).HandleAsync(new ListWidgetsQuery(), ct));

        Assert.Equal(id, listed.Id);
        Assert.Equal(createdTime, listed.CreatedTimeUtc);
        Assert.Equal("Updated links", listed.Title);
        Assert.Equal(updatedRequest.ContentCode, listed.ContentCode);
        Assert.Equal(999, listed.DisplayOrder);
        Assert.False(listed.IsEnabled);

        var delete = new DeleteWidgetCommandHandler(db, NullLogger<DeleteWidgetCommandHandler>.Instance);
        Assert.Equal(OperationCode.Done, await delete.HandleAsync(new DeleteWidgetCommand(id), ct));
        Assert.Empty(await new ListWidgetsQueryHandler(db).HandleAsync(new ListWidgetsQuery(), ct));
        Assert.Null(await new GetWidgetQueryHandler(db).HandleAsync(new GetWidgetQuery(id), ct));
        Assert.Equal(OperationCode.ObjectNotFound,
            await update.HandleAsync(new UpdateWidgetCommand(id, updatedRequest), ct));
        Assert.Equal(OperationCode.ObjectNotFound, await delete.HandleAsync(new DeleteWidgetCommand(id), ct));
    }
}
