using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class GetTagQuery : IRequest<Tag>
    {
        public GetTagQuery(string normalizedName)
        {
            NormalizedName = normalizedName;
        }

        public string NormalizedName { get; set; }
    }

    public class GetTagQueryHandler : IRequestHandler<GetTagQuery, Tag>
    {
        private readonly IRepository<TagEntity> _tagRepo;

        public GetTagQueryHandler(IRepository<TagEntity> tagRepo)
        {
            _tagRepo = tagRepo;
        }

        public Task<Tag> Handle(GetTagQuery request, CancellationToken cancellationToken)
        {
            var tag = _tagRepo.SelectFirstOrDefault(new TagSpec(request.NormalizedName), Tag.EntitySelector);
            return Task.FromResult(tag);
        }
    }
}
