using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth
{
    public class AccountExistsQuery : IRequest<bool>
    {
        public AccountExistsQuery(string username)
        {
            Username = username;
        }

        public string Username { get; set; }
    }

    public class AccountExistsQueryHandler : IRequestHandler<AccountExistsQuery, bool>
    {
        private readonly IRepository<LocalAccountEntity> _accountRepo;

        public AccountExistsQueryHandler(IRepository<LocalAccountEntity> accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public Task<bool> Handle(AccountExistsQuery request, CancellationToken cancellationToken)
        {
            var exist = _accountRepo.Any(p => p.Username == request.Username.ToLower());
            return Task.FromResult(exist);
        }
    }
}
