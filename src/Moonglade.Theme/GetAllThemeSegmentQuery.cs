using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Theme;

public record GetAllThemeSegmentQuery : IRequest<List<ThemeSegment>>;

public class GetAllThemeSegmentQueryHandler(IRepository<BlogThemeEntity> repo) : IRequestHandler<GetAllThemeSegmentQuery, List<ThemeSegment>>
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