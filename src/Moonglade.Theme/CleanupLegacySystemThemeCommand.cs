using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Theme;

public class CleanupLegacySystemThemeCommand : IRequest;

public class CleanupLegacySystemThemeCommandHandler(MoongladeRepository<BlogThemeEntity> repo)
    : IRequestHandler<CleanupLegacySystemThemeCommand>
{
    public async Task Handle(CleanupLegacySystemThemeCommand request, CancellationToken ct)
    {
        var existingThemes = await repo.ListAsync(new ThemeByTypeSpec(ThemeType.System), ct);
        await repo.DeleteRangeAsync(existingThemes, ct);
    }
}