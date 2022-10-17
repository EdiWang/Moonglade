using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record GetAccountQuery(Guid Id) : IRequest<Account>;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, Account>
{
    private readonly IRepository<LocalAccountEntity> _repo;

    public GetAccountQueryHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    public async Task<Account> Handle(GetAccountQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(request.Id, ct);
        var item = new Account(entity);
        return item;
    }
}