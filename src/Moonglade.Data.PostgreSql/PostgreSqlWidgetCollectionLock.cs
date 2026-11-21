using Microsoft.EntityFrameworkCore;

namespace Moonglade.Data.PostgreSql;

public sealed class PostgreSqlWidgetCollectionLock(BlogDbContext db) : IWidgetCollectionLock
{
    public Task AcquireAsync(bool write, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("The widget lock requires a transaction.");

        return db.Database.ExecuteSqlRawAsync(write
            ? "SELECT pg_advisory_xact_lock(5570754044645364807)"
            : "SELECT pg_advisory_xact_lock_shared(5570754044645364807)", ct);
    }
}
