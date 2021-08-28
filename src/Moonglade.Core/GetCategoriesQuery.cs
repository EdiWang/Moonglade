using MediatR;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class GetCategoriesQuery : IRequest<IReadOnlyList<Category>>
    {
    }

    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
    {
        private readonly IRepository<CategoryEntity> _catRepo;
        private readonly IBlogCache _cache;

        public GetCategoriesQueryHandler(IRepository<CategoryEntity> catRepo, IBlogCache cache)
        {
            _catRepo = catRepo;
            _cache = cache;
        }

        public Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            return _cache.GetOrCreateAsync(CacheDivision.General, "allcats", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                var list = await _catRepo.SelectAsync(Category.EntitySelector);
                return list;
            });
        }
    }
}
