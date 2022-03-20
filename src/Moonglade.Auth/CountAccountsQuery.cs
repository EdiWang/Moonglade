using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record CountAccountsQuery : IRequest<int>;

public class CountAccountsQueryHandler : RequestHandler<CountAccountsQuery, int>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public CountAccountsQueryHandler(IRepository<LocalAccountEntity> accountRepo) => _accountRepo = accountRepo;

    protected override int Handle(CountAccountsQuery request) => _accountRepo.Count();
}