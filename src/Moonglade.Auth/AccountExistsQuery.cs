using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record AccountExistsQuery(string Username) : IRequest<bool>;

public class AccountExistsQueryHandler : IRequestHandler<AccountExistsQuery, bool>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public AccountExistsQueryHandler(IRepository<LocalAccountEntity> accountRepo) => _accountRepo = accountRepo;

    public Task<bool> Handle(AccountExistsQuery request, CancellationToken ct) =>
        _accountRepo.AnyAsync(p => p.Username == request.Username.ToLower(), ct);
}