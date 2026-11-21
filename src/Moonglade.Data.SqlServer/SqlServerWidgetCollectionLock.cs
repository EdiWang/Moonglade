using Microsoft.EntityFrameworkCore;

namespace Moonglade.Data.SqlServer;

public sealed class SqlServerWidgetCollectionLock(BlogDbContext db) : IWidgetCollectionLock
{
    public Task AcquireAsync(bool write, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("The widget lock requires a transaction.");

        var mode = write ? "Exclusive" : "Shared";
        return db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = N'Moonglade.Widgets', @LockMode = {mode},
                @LockOwner = 'Transaction', @LockTimeout = 30000;
            IF @result < 0 THROW 51001, 'Could not acquire the widget collection lock.', 1;
            """, ct);
    }
}
