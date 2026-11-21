using Moonglade.Data;

namespace Moonglade.Web.Tests.TestSupport;

internal sealed class SqliteWidgetCollectionLock(BlogDbContext db) : IWidgetCollectionLock
{
    public Task AcquireAsync(bool write, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Assert.NotNull(db.Database.CurrentTransaction);
        return Task.CompletedTask;
    }
}
