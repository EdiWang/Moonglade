using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record CountAccountsQuery : IRequest<int>;

public class CountAccountsQueryHandler : RequestHandler<CountAccountsQuery, int>
{
    private readonly IRepository<LocalAccountEntity> _repo;

    public CountAccountsQueryHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    protected override int Handle(CountAccountsQuery request) => _repo.Count();
}