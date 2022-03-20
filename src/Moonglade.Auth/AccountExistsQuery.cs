using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record AccountExistsQuery(string Username) : IRequest<bool>;

public class AccountExistsQueryHandler : RequestHandler<AccountExistsQuery, bool>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public AccountExistsQueryHandler(IRepository<LocalAccountEntity> accountRepo) => _accountRepo = accountRepo;

    protected override bool Handle(AccountExistsQuery request) => _accountRepo.Any(p => p.Username == request.Username.ToLower());
}