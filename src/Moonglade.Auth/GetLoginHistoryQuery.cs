using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Auth;

public record GetLoginHistoryQuery : IRequest<IReadOnlyList<LoginHistoryEntity>>;

public class GetLoginHistoryQueryHandler(IRepository<LoginHistoryEntity> repo) : IRequestHandler<GetLoginHistoryQuery, IReadOnlyList<LoginHistoryEntity>>
{
    public async Task<IReadOnlyList<LoginHistoryEntity>> Handle(GetLoginHistoryQuery request, CancellationToken ct)
    {
        var history = await repo.ListAsync(new LoginHistorySpec(10));
        return history;
    }
}