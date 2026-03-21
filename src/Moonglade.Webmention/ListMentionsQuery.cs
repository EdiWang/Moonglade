using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Webmention;

public record ListMentionsQuery(
    int PageSize = 10,
    int PageIndex = 1,
    string? Domain = null,
    string? SourceTitle = null,
    string? TargetPostTitle = null,
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null) : IQuery<(List<MentionEntity> Mentions, int TotalCount)>;

public class ListMentionsQueryHandler(IRepositoryBase<MentionEntity> repo) :
    IQueryHandler<ListMentionsQuery, (List<MentionEntity> Mentions, int TotalCount)>
{
    public async Task<(List<MentionEntity> Mentions, int TotalCount)> HandleAsync(ListMentionsQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
        }

        if (request.PageIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageIndex)} can not be less than 1, current value: {request.PageIndex}.");
        }

        var pagingSpec = new MentionPagingSpec(
            request.PageSize, request.PageIndex,
            request.Domain, request.SourceTitle, request.TargetPostTitle,
            request.StartTimeUtc, request.EndTimeUtc);
        var entities = await repo.ListAsync(pagingSpec, ct);

        var countSpec = new MentionCountSpec(
            request.Domain, request.SourceTitle, request.TargetPostTitle,
            request.StartTimeUtc, request.EndTimeUtc);
        var totalCount = await repo.CountAsync(countSpec, ct);

        return (entities, totalCount);
    }
}