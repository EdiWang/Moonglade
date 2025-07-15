using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record DeleteThemeCommand(int Id) : ICommand<OperationCode>;

public class DeleteThemeCommandHandler(MoongladeRepository<BlogThemeEntity> repo) : ICommandHandler<DeleteThemeCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteThemeCommand request, CancellationToken ct)
    {
        var theme = await repo.GetByIdAsync(request.Id, ct);
        if (null == theme) return OperationCode.ObjectNotFound;
        if (theme.ThemeType == ThemeType.System) return OperationCode.Canceled;

        await repo.DeleteAsync(theme, ct);
        return OperationCode.Done;
    }
}