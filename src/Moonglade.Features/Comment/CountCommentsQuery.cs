using LiteBus.Queries.Abstractions;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Comment;

public record CountCommentsQuery(string SearchTerm = null) : IQuery<int>;

public class CountCommentsQueryHandler(IRepositoryBase<CommentEntity> repo) : IQueryHandler<CountCommentsQuery, int>
{
    public Task<int> HandleAsync(CountCommentsQuery request, CancellationToken ct)
    {
        return repo.CountAsync(new CommentCountSpec(request.SearchTerm), ct);
    }
}