using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record DeleteThemeCommand(int Id) : ICommand<OperationCode>;

public class DeleteThemeCommandHandler(BlogDbContext db) : ICommandHandler<DeleteThemeCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteThemeCommand request, CancellationToken ct)
    {
        var theme = await db.BlogTheme.FindAsync([request.Id], ct);
        if (null == theme) return OperationCode.ObjectNotFound;
        if (theme.ThemeType == ThemeType.System) return OperationCode.Canceled;

        db.BlogTheme.Remove(theme);
        await db.SaveChangesAsync(ct);
        return OperationCode.Done;
    }
}