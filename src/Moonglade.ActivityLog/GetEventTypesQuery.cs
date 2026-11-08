using LiteBus.Queries.Abstractions;

namespace Moonglade.ActivityLog;

public record GetEventTypesQuery : IQuery<List<ActivityEventDefinition>>;

public class GetEventTypesQueryHandler : IQueryHandler<GetEventTypesQuery, List<ActivityEventDefinition>>
{
    public Task<List<ActivityEventDefinition>> HandleAsync(GetEventTypesQuery request, CancellationToken ct) =>
        Task.FromResult(ActivityEvents.All.OrderBy(definition => definition.Group, StringComparer.Ordinal)
            .ThenBy(definition => definition.Key, StringComparer.Ordinal).ToList());
}
