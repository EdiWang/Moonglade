using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record CountAccountsQuery : IRequest<int>;

public class CountAccountsQueryHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<CountAccountsQuery, int>
{
    public Task<int> Handle(CountAccountsQuery request, CancellationToken ct) => repo.CountAsync(ct: ct);
}