using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record GetAccountsQuery : IRequest<IReadOnlyList<Account>>;

public class GetAccountsQueryHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<GetAccountsQuery, IReadOnlyList<Account>>
{
    public Task<IReadOnlyList<Account>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        return repo.SelectAsync(p => new Account
        {
            Id = p.Id,
            CreateTimeUtc = p.CreateTimeUtc,
            Username = p.Username
        }, ct);
    }
}