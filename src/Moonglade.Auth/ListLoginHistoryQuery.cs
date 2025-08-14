using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Auth;

public record ListLoginHistoryQuery : IQuery<List<LoginHistoryEntity>>;

public class ListLoginHistoryQueryHandler(MoongladeRepository<LoginHistoryEntity> repo) : IQueryHandler<ListLoginHistoryQuery, List<LoginHistoryEntity>>
{
    public async Task<List<LoginHistoryEntity>> HandleAsync(ListLoginHistoryQuery request, CancellationToken ct)
    {
        var history = await repo.ListAsync(new LoginHistorySpec(10), ct);
        return history;
    }
}