using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.StatisticFeature
{
    public class UpdateStatisticCommand : IRequest
    {
        public UpdateStatisticCommand(Guid postId, bool isLike)
        {
            PostId = postId;
            IsLike = isLike;
        }

        public Guid PostId { get; set; }

        public bool IsLike { get; set; }
    }

    public class UpdateStatisticCommandHandler : IRequestHandler<UpdateStatisticCommand>
    {
        private readonly IRepository<PostExtensionEntity> _postExtensionRepo;

        public UpdateStatisticCommandHandler(IRepository<PostExtensionEntity> postExtensionRepo)
        {
            _postExtensionRepo = postExtensionRepo;
        }

        public async Task<Unit> Handle(UpdateStatisticCommand request, CancellationToken cancellationToken)
        {
            var pp = await _postExtensionRepo.GetAsync(request.PostId);
            if (pp is null) return Unit.Value;

            if (request.IsLike)
            {
                if (pp.Likes >= int.MaxValue) return Unit.Value;
                pp.Likes += 1;
            }
            else
            {
                if (pp.Hits >= int.MaxValue) return Unit.Value;
                pp.Hits += 1;
            }

            await _postExtensionRepo.UpdateAsync(pp);
            return Unit.Value;
        }
    }
}
