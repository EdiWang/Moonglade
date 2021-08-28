using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class DeleteTagCommand : IRequest<OperationCode>
    {
        public DeleteTagCommand(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
    }

    public class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand, OperationCode>
    {
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IBlogAudit _audit;

        public DeleteTagCommandHandler(IRepository<TagEntity> tagRepo, IRepository<PostTagEntity> postTagRepo, IBlogAudit audit)
        {
            _tagRepo = tagRepo;
            _postTagRepo = postTagRepo;
            _audit = audit;
        }

        public async Task<OperationCode> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
        {
            var exists = _tagRepo.Any(c => c.Id == request.Id);
            if (!exists) return OperationCode.ObjectNotFound;

            // 1. Delete Post-Tag Association
            var postTags = await _postTagRepo.GetAsync(new PostTagSpec(request.Id));
            await _postTagRepo.DeleteAsync(postTags);

            // 2. Delte Tag itslef
            await _tagRepo.DeleteAsync(request.Id);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.TagDeleted, $"Tag id '{request.Id}' is deleted");

            return OperationCode.Done;
        }
    }
}
