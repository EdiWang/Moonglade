using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Widgets;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Tests.Handlers;

public class WidgetPlacementTests
{
    [Fact]
    public async Task List_OrdersByRegionPositionUtcAndCanonicalId_AndIncludesDisabledWidgets()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(ct);
        var timestamp = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var canonicalFirst = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var canonicalLast = Guid.Parse("00000001-0000-0000-0000-000000000000");
        var earlierTime = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var earlierPosition = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var laterRegion = Guid.Parse("10000000-0000-0000-0000-000000000003");
        db.Widget.AddRange(
            Create(canonicalLast, WidgetRegion.BeforeProfile, 5, timestamp.AddTicks(1)),
            Create(laterRegion, WidgetRegion.AfterCategories, 0, timestamp),
            Create(canonicalFirst, WidgetRegion.BeforeProfile, 5, timestamp.AddTicks(1)),
            Create(earlierPosition, WidgetRegion.BeforeProfile, 4, timestamp.AddDays(1)),
            Create(earlierTime, WidgetRegion.BeforeProfile, 5, timestamp));
        await db.SaveChangesAsync(ct);

        var result = await new ListWidgetsQueryHandler(db).HandleAsync(new ListWidgetsQuery(), ct);

        Assert.Equal([earlierPosition, earlierTime, canonicalFirst, canonicalLast, laterRegion], result.Select(widget => widget.Id));
        Assert.All(result, widget => Assert.False(widget.IsEnabled));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(4, 0)]
    [InlineData(3, -1)]
    public async Task InvalidPlacement_CannotCreateOrMutateAStoredWidget(int region, int order)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(ct);
        var request = new EditWidgetRequest
        {
            Title = "Invalid placement",
            WidgetType = WidgetType.LinkList,
            ContentCode = "[]",
            Region = (WidgetRegion)region,
            SortOrder = order
        };
        var create = new CreateWidgetCommandHandler(db, NullLogger<CreateWidgetCommandHandler>.Instance);
        await Assert.ThrowsAsync<ValidationException>(() => create.HandleAsync(new CreateWidgetCommand(request), ct));
        Assert.Empty(await db.Widget.ToListAsync(ct));
        var stored = Create(Guid.NewGuid(), WidgetRegion.BeforeTags, 10, DateTime.UtcNow);
        db.Widget.Add(stored);
        await db.SaveChangesAsync(ct);

        var update = new UpdateWidgetCommandHandler(db, NullLogger<UpdateWidgetCommandHandler>.Instance);
        await Assert.ThrowsAsync<ValidationException>(() => update.HandleAsync(new UpdateWidgetCommand(stored.Id, request), ct));
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var unchanged = await db.Widget.SingleAsync(ct);
        Assert.Equal("Fixture", unchanged.Title);
        Assert.Equal(WidgetRegion.BeforeTags, unchanged.Region);
        Assert.Equal(10, unchanged.SortOrder);
    }

    private static WidgetEntity Create(Guid id, WidgetRegion region, int order, DateTime timestamp) => new()
    {
        Id = id,
        Title = "Fixture",
        WidgetType = WidgetType.LinkList,
        ContentType = WidgetContentType.JSON,
        ContentCode = "[]",
        Region = region,
        SortOrder = order,
        CreatedTimeUtc = timestamp,
        IsEnabled = false
    };
}
