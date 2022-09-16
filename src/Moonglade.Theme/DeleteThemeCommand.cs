using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Theme;

public record DeleteThemeCommand(int Id) : IRequest<OperationCode>;

public class DeleteThemeCommandHandler : IRequestHandler<DeleteThemeCommand, OperationCode>
{
    private readonly IRepository<BlogThemeEntity> _repo;

    public DeleteThemeCommandHandler(IRepository<BlogThemeEntity> repo) => _repo = repo;

    public async Task<OperationCode> Handle(DeleteThemeCommand request, CancellationToken cancellationToken)
    {
        var theme = await _repo.GetAsync(request.Id, cancellationToken);
        if (null == theme) return OperationCode.ObjectNotFound;
        if (theme.ThemeType == ThemeType.System) return OperationCode.Canceled;

        await _repo.DeleteAsync(request.Id, cancellationToken);
        return OperationCode.Done;
    }
}