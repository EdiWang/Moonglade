using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record GetLoginHistoryQuery : IRequest<IReadOnlyList<LoginHistoryEntity>>;

public class GetLoginHistoryQueryHandler(IRepository<LoginHistoryEntity> repo) : IRequestHandler<GetLoginHistoryQuery, IReadOnlyList<LoginHistoryEntity>>
{
    public async Task<IReadOnlyList<LoginHistoryEntity>> Handle(GetLoginHistoryQuery request, CancellationToken ct)
    {
        var history = await repo.ListAsync(ct);
        return history;
    }
}