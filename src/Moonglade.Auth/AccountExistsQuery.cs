using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record AccountExistsQuery(string Username) : IRequest<bool>;

public class AccountExistsQueryHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<AccountExistsQuery, bool>
{
    public Task<bool> Handle(AccountExistsQuery request, CancellationToken ct) =>
        repo.AnyAsync(p => p.Username == request.Username.ToLower(), ct);
}