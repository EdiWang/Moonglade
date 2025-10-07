using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features.Comment;

public record CountCommentsQuery : IQuery<int>;

public class CountCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IQueryHandler<CountCommentsQuery, int>
{
    public Task<int> HandleAsync(CountCommentsQuery request, CancellationToken ct) => repo.CountAsync(ct);
}