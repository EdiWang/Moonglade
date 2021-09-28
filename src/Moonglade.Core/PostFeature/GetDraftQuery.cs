using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.PostFeature
{
    public class GetDraftQuery : IRequest<Post>
    {
        public GetDraftQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public class GetDraftQueryHandler : IRequestHandler<GetDraftQuery, Post>
    {
        private readonly IRepository<PostEntity> _postRepo;

        public GetDraftQueryHandler(IRepository<PostEntity> postRepo)
        {
            _postRepo = postRepo;
        }

        public Task<Post> Handle(GetDraftQuery request, CancellationToken cancellationToken)
        {
            var spec = new PostSpec(request.Id);
            var post = _postRepo.SelectFirstOrDefaultAsync(spec, Post.EntitySelector);
            return post;
        }
    }
}
