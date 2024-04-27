using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Auth;

public record GetLoginHistoryQuery : IRequest<List<LoginHistoryEntity>>;

public class GetLoginHistoryQueryHandler(MoongladeRepository<LoginHistoryEntity> repo) : IRequestHandler<GetLoginHistoryQuery, List<LoginHistoryEntity>>
{
    public async Task<List<LoginHistoryEntity>> Handle(GetLoginHistoryQuery request, CancellationToken ct)
    {
        var history = await repo.ListAsync(new LoginHistorySpec(10), ct);
        return history;
    }
}