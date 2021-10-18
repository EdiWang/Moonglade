using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.StatisticFeature
{
    public class GetStatisticQuery : IRequest<(int Hits, int Likes)>
    {
        public GetStatisticQuery(Guid postId)
        {
            PostId = postId;
        }

        public Guid PostId { get; set; }
    }

    public class GetStatisticQueryHandler : IRequestHandler<GetStatisticQuery, (int Hits, int Likes)>
    {
        private readonly IRepository<PostExtensionEntity> _postExtensionRepo;

        public GetStatisticQueryHandler(IRepository<PostExtensionEntity> postExtensionRepo)
        {
            _postExtensionRepo = postExtensionRepo;
        }

        public async Task<(int Hits, int Likes)> Handle(GetStatisticQuery request, CancellationToken cancellationToken)
        {
            var pp = await _postExtensionRepo.GetAsync(request.PostId);
            return (pp.Hits, pp.Likes);
        }
    }
}
