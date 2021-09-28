using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Auth
{
    public class GetAccountsQuery : IRequest<IReadOnlyList<Account>>
    {

    }

    public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, IReadOnlyList<Account>>
    {
        private readonly IRepository<LocalAccountEntity> _accountRepo;

        public GetAccountsQueryHandler(IRepository<LocalAccountEntity> accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public Task<IReadOnlyList<Account>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
        {
            var list = _accountRepo.SelectAsync(p => new Account
            {
                Id = p.Id,
                CreateTimeUtc = p.CreateTimeUtc,
                LastLoginIp = p.LastLoginIp,
                LastLoginTimeUtc = p.LastLoginTimeUtc,
                Username = p.Username
            });

            return list;
        }
    }
}
