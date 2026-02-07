using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Auth;

public record ListLoginHistoryQuery : IQuery<List<LoginHistoryEntity>>;

public class ListLoginHistoryQueryHandler(IRepositoryBase<LoginHistoryEntity> repo) : IQueryHandler<ListLoginHistoryQuery, List<LoginHistoryEntity>>
{
    public Task<List<LoginHistoryEntity>> HandleAsync(ListLoginHistoryQuery request, CancellationToken ct) =>
        repo.ListAsync(new LoginHistorySpec(10), ct);
}