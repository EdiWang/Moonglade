using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;

namespace Moonglade.ActivityLog;

public record GetMetaDataByActivityLogIdQuery(long ActivityLogId) : IQuery<string?>;

public class GetMetaDataByActivityLogIdQueryHandler(IRepositoryBase<ActivityLogEntity> repository)
    : IQueryHandler<GetMetaDataByActivityLogIdQuery, string?>
{
    public async Task<string?> HandleAsync(GetMetaDataByActivityLogIdQuery request, CancellationToken ct)
    {
        if (request.ActivityLogId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.ActivityLogId)} must be greater than 0, current value: {request.ActivityLogId}.");
        }

        var entity = await repository.GetByIdAsync(request.ActivityLogId, ct);

        return entity?.MetaData;
    }
}
