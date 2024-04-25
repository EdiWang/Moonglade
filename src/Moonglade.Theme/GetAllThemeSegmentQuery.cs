using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record GetAllThemeSegmentQuery : IRequest<List<ThemeSegment>>;

public class GetAllThemeSegmentQueryHandler(MoongladeRepository<BlogThemeEntity> repo) : IRequestHandler<GetAllThemeSegmentQuery, List<ThemeSegment>>
{
    public Task<List<ThemeSegment>> Handle(GetAllThemeSegmentQuery request, CancellationToken ct)
    {
        return repo.SelectAsync(p => new ThemeSegment
        {
            Id = p.Id,
            Name = p.ThemeName
        }, ct);
    }
}