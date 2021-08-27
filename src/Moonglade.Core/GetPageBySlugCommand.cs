using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class GetPageBySlugCommand : IRequest<BlogPage>
    {
        public GetPageBySlugCommand(string slug)
        {
            Slug = slug;
        }

        public string Slug { get; set; }
    }

    public class GetPageBySlugCommandHandler : IRequestHandler<GetPageBySlugCommand, BlogPage>
    {
        private readonly IRepository<PageEntity> _pageRepo;

        public GetPageBySlugCommandHandler(IRepository<PageEntity> pageRepo)
        {
            _pageRepo = pageRepo;
        }

        public async Task<BlogPage> Handle(GetPageBySlugCommand request, CancellationToken cancellationToken)
        {
            var lower = request.Slug.ToLower();
            var entity = await _pageRepo.GetAsync(p => p.Slug == lower);
            if (entity == null) return null;

            var item = new BlogPage(entity);
            return item;
        }
    }
}
