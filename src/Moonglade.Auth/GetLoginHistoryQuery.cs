using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Auth;

public record GetLoginHistoryQuery : IRequest<List<LoginHistoryEntity>>;

public class GetLoginHistoryQueryHandler(IRepository<LoginHistoryEntity> repo) : IRequestHandler<GetLoginHistoryQuery, List<LoginHistoryEntity>>
{
    public async Task<List<LoginHistoryEntity>> Handle(GetLoginHistoryQuery request, CancellationToken ct)
    {
        var history = await repo.ListAsync(new LoginHistorySpec(10));
        return history;
    }
}