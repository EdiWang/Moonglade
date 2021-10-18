using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth
{
    public class GetAccountQuery : IRequest<Account>
    {
        public GetAccountQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, Account>
    {
        private readonly IRepository<LocalAccountEntity> _accountRepo;

        public GetAccountQueryHandler(IRepository<LocalAccountEntity> accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public async Task<Account> Handle(GetAccountQuery request, CancellationToken cancellationToken)
        {
            var entity = await _accountRepo.GetAsync(request.Id);
            var item = new Account(entity);
            return item;
        }
    }
}
