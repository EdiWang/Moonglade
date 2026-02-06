using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Comment;

public record CountCommentsQuery : IQuery<int>;

public class CountCommentsQueryHandler(IRepositoryBase<CommentEntity> repo) : IQueryHandler<CountCommentsQuery, int>
{
    public Task<int> HandleAsync(CountCommentsQuery request, CancellationToken ct) => repo.CountAsync(ct);
}