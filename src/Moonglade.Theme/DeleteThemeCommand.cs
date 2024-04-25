using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record DeleteThemeCommand(int Id) : IRequest<OperationCode>;

public class DeleteThemeCommandHandler(MoongladeRepository<BlogThemeEntity> repo) : IRequestHandler<DeleteThemeCommand, OperationCode>
{
    public async Task<OperationCode> Handle(DeleteThemeCommand request, CancellationToken ct)
    {
        var theme = await repo.GetByIdAsync(request.Id, ct);
        if (null == theme) return OperationCode.ObjectNotFound;
        if (theme.ThemeType == ThemeType.System) return OperationCode.Canceled;

        await repo.DeleteAsync(theme, ct);
        return OperationCode.Done;
    }
}