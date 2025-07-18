using LiteBus.Queries.Abstractions;
using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Comments;

public record CountCommentsQuery : IQuery<int>;

public class CountCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IQueryHandler<CountCommentsQuery, int>
{
    public Task<int> HandleAsync(CountCommentsQuery request, CancellationToken ct) => repo.CountAsync(ct);
}