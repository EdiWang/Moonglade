using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public class CountAccountsQuery : IRequest<int>
{

}

public class CountAccountsQueryHandler : IRequestHandler<CountAccountsQuery, int>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public CountAccountsQueryHandler(IRepository<LocalAccountEntity> accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public Task<int> Handle(CountAccountsQuery request, CancellationToken cancellationToken)
    {
        var count = _accountRepo.Count();
        return Task.FromResult(count);
    }
}