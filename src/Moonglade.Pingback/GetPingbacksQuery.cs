using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Pingback
{
    public class GetPingbacksQuery : IRequest<IReadOnlyList<PingbackEntity>>
    {

    }

    public class GetPingbacksQueryHandler : IRequestHandler<GetPingbacksQuery, IReadOnlyList<PingbackEntity>>
    {
        private readonly IRepository<PingbackEntity> _pingbackRepo;

        public GetPingbacksQueryHandler(IRepository<PingbackEntity> pingbackRepo)
        {
            _pingbackRepo = pingbackRepo;
        }

        public Task<IReadOnlyList<PingbackEntity>> Handle(GetPingbacksQuery request, CancellationToken cancellationToken)
        {
            return _pingbackRepo.GetAsync();
        }
    }
}
