using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Theme;

public record DeleteThemeCommand(int Id) : IRequest<OperationCode>;

public class DeleteThemeCommandHandler : IRequestHandler<DeleteThemeCommand, OperationCode>
{
    private readonly IRepository<BlogThemeEntity> _themeRepo;

    public DeleteThemeCommandHandler(IRepository<BlogThemeEntity> themeRepo)
    {
        _themeRepo = themeRepo;
    }

    public async Task<OperationCode> Handle(DeleteThemeCommand request, CancellationToken cancellationToken)
    {
        var theme = await _themeRepo.GetAsync(request.Id);
        if (null == theme) return OperationCode.ObjectNotFound;
        if (theme.ThemeType == ThemeType.System) return OperationCode.Canceled;

        await _themeRepo.DeleteAsync(request.Id);
        return OperationCode.Done;
    }
}