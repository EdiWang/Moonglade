using LiteBus.Queries.Abstractions;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Comment;

public record CountCommentsQuery(
    string Username = null,
    string Email = null,
    string CommentContent = null,
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null) : IQuery<int>;

public class CountCommentsQueryHandler(IRepositoryBase<CommentEntity> repo) : IQueryHandler<CountCommentsQuery, int>
{
    public Task<int> HandleAsync(CountCommentsQuery request, CancellationToken ct)
    {
        return repo.CountAsync(new CommentCountSpec(request.Username, request.Email, request.CommentContent, request.StartTimeUtc, request.EndTimeUtc), ct);
    }
}