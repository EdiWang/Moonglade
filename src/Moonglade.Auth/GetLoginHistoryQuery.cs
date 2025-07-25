using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Auth;

public record GetLoginHistoryQuery : IQuery<List<LoginHistoryEntity>>;

public class GetLoginHistoryQueryHandler(MoongladeRepository<LoginHistoryEntity> repo) : IQueryHandler<GetLoginHistoryQuery, List<LoginHistoryEntity>>
{
    public async Task<List<LoginHistoryEntity>> HandleAsync(GetLoginHistoryQuery request, CancellationToken ct)
    {
        var history = await repo.ListAsync(new LoginHistorySpec(10), ct);
        return history;
    }
}