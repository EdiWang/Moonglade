using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Comments;

public record CountCommentsQuery : IRequest<int>;

public class CountCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IRequestHandler<CountCommentsQuery, int>
{
    public Task<int> Handle(CountCommentsQuery request, CancellationToken ct) => repo.CountAsync(ct);
}