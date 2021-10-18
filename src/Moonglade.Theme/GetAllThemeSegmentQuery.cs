using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Theme
{
    public class GetAllThemeSegmentQuery : IRequest<IReadOnlyList<ThemeSegment>>
    {
    }

    public class GetAllThemeSegmentQueryHandler : IRequestHandler<GetAllThemeSegmentQuery, IReadOnlyList<ThemeSegment>>
    {
        private readonly IRepository<BlogThemeEntity> _themeRepo;

        public GetAllThemeSegmentQueryHandler(IRepository<BlogThemeEntity> themeRepo)
        {
            _themeRepo = themeRepo;
        }

        public Task<IReadOnlyList<ThemeSegment>> Handle(GetAllThemeSegmentQuery request, CancellationToken cancellationToken)
        {
            return _themeRepo.SelectAsync(p => new ThemeSegment
            {
                Id = p.Id,
                Name = p.ThemeName
            });
        }
    }
}
