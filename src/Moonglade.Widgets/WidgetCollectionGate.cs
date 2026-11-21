using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public sealed class WidgetCollectionGate(BlogDbContext db, IWidgetCollectionLock collectionLock)
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> write, CancellationToken ct)
        => ExecuteAsync(write, true, ct);

    public Task<T> ReadAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken ct)
        => ExecuteAsync(read, false, ct);

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, bool write, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null || db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Widget collection operations require an independent unit of work.");

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async token =>
        {
            DetachWidgets();
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            try
            {
                await collectionLock.AcquireAsync(write, token);
                var result = await operation(token);
                token.ThrowIfCancellationRequested();
                await transaction.CommitAsync(token);
                return result;
            }
            finally
            {
                DetachWidgets();
            }
        }, ct);
    }

    private void DetachWidgets()
    {
        foreach (var entry in db.ChangeTracker.Entries<WidgetEntity>().ToArray())
            entry.State = EntityState.Detached;
    }
}
