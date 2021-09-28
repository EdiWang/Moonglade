using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.PageFeature
{
    public class GetPageByIdQuery : IRequest<BlogPage>
    {
        public GetPageByIdQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public class GetPageByIdQueryHandler : IRequestHandler<GetPageByIdQuery, BlogPage>
    {
        private readonly IRepository<PageEntity> _pageRepo;

        public GetPageByIdQueryHandler(IRepository<PageEntity> pageRepo)
        {
            _pageRepo = pageRepo;
        }

        public async Task<BlogPage> Handle(GetPageByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _pageRepo.GetAsync(request.Id);
            if (entity == null) return null;

            var item = new BlogPage(entity);
            return item;
        }
    }
}
