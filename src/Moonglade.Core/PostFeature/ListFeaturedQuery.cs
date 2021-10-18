using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature
{
    public class ListFeaturedQuery : IRequest<IReadOnlyList<PostDigest>>
    {
        public ListFeaturedQuery(int pageSize, int pageIndex)
        {
            PageSize = pageSize;
            PageIndex = pageIndex;
        }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }
    }

    public class ListFeaturedQueryHandler : IRequestHandler<ListFeaturedQuery, IReadOnlyList<PostDigest>>
    {
        private readonly IRepository<PostEntity> _postRepo;

        public ListFeaturedQueryHandler(IRepository<PostEntity> postRepo)
        {
            _postRepo = postRepo;
        }

        public Task<IReadOnlyList<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken cancellationToken)
        {
            Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

            var posts = _postRepo.SelectAsync(new FeaturedPostSpec(request.PageSize, request.PageIndex), PostDigest.EntitySelector);
            return posts;
        }
    }
}
