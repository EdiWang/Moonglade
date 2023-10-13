using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Comments;

public record CountCommentsQuery : IRequest<int>;

public class CountCommentsQueryHandler(IRepository<CommentEntity> repo) : IRequestHandler<CountCommentsQuery, int>
{
    public Task<int> Handle(CountCommentsQuery request, CancellationToken ct) => repo.CountAsync(ct: ct);
}