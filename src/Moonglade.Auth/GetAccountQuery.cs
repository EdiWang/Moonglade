using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record GetAccountQuery(Guid Id) : IRequest<Account>;

public class GetAccountQueryHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<GetAccountQuery, Account>
{
    public async Task<Account> Handle(GetAccountQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAsync(request.Id, ct);
        var item = new Account(entity);
        return item;
    }
}