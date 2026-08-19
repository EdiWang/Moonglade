using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Setup.Tests;

public sealed class UtcDateTimeContractTests
{
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task SaveChanges_RejectsNonUtcValuesForUtcProperties(DateTimeKind dateTimeKind)
    {
        await using var context = CreateContext();
        context.ActivityLog.Add(new ActivityLogEntity
        {
            EventId = 1,
            EventTimeUtc = DateTime.SpecifyKind(
                new DateTime(2026, 8, 15, 1, 2, 3),
                dateTimeKind)
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Contains(
            $"{nameof(ActivityLogEntity)}.{nameof(ActivityLogEntity.EventTimeUtc)}",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveChanges_AcceptsUtcValuesForUtcProperties()
    {
        await using var context = CreateContext();
        context.ActivityLog.Add(new ActivityLogEntity
        {
            EventId = 1,
            EventTimeUtc = new DateTime(2026, 8, 15, 1, 2, 3, DateTimeKind.Utc)
        });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, await context.ActivityLog.CountAsync(TestContext.Current.CancellationToken));
    }

    private static BlogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }
}
