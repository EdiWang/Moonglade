using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.PageFeature
{
    public class ListPageSegmentQuery : IRequest<IReadOnlyList<PageSegment>>
    {
    }

    public class ListPageSegmentQueryHandler : IRequestHandler<ListPageSegmentQuery, IReadOnlyList<PageSegment>>
    {
        private readonly IRepository<PageEntity> _pageRepo;

        public ListPageSegmentQueryHandler(IRepository<PageEntity> pageRepo)
        {
            _pageRepo = pageRepo;
        }

        public Task<IReadOnlyList<PageSegment>> Handle(ListPageSegmentQuery request, CancellationToken cancellationToken)
        {
            return _pageRepo.SelectAsync(page => new PageSegment
            {
                Id = page.Id,
                CreateTimeUtc = page.CreateTimeUtc,
                Slug = page.Slug,
                Title = page.Title,
                IsPublished = page.IsPublished
            });
        }
    }
}
