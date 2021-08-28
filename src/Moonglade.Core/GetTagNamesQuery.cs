using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class GetTagNamesQuery : IRequest<IReadOnlyList<string>>
    {
    }

    public class GetTagNamesQueryHandler : IRequestHandler<GetTagNamesQuery, IReadOnlyList<string>>
    {
        private readonly IRepository<TagEntity> _tagRepo;

        public GetTagNamesQueryHandler(IRepository<TagEntity> tagRepo)
        {
            _tagRepo = tagRepo;
        }

        public Task<IReadOnlyList<string>> Handle(GetTagNamesQuery request, CancellationToken cancellationToken)
        {
            return _tagRepo.SelectAsync(t => t.DisplayName);
        }
    }
}
