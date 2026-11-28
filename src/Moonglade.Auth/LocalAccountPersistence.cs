#nullable enable

using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Auth;

internal static class LocalAccountPersistence
{
    public static Task SaveWithNewRevisionAsync(BlogDbContext db, BlogAccountEntity account, CancellationToken ct,
        Func<BlogAccountEntity, DbUpdateException, Task>? onUpdateError = null)
    {
        account.Revision = Guid.NewGuid();
        return SaveAndDetachAsync(db, account, ct, onUpdateError);
    }

    public static async Task SaveAndDetachAsync(BlogDbContext db, BlogAccountEntity account, CancellationToken ct,
        Func<BlogAccountEntity, DbUpdateException, Task>? onUpdateError = null)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (onUpdateError is not null && exception is not DbUpdateConcurrencyException)
        {
            await onUpdateError(account, exception);
            throw;
        }
        finally
        {
            // A failed account write must not be retried by a later unrelated save.
            db.Entry(account).State = EntityState.Detached;
        }
    }
}
