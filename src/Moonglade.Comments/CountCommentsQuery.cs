using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Comments;

public record CountCommentsQuery : IRequest<int>;

public class CountCommentsQueryHandler : IRequestHandler<CountCommentsQuery, int>
{
    private readonly IRepository<CommentEntity> _repo;

    public CountCommentsQueryHandler(IRepository<CommentEntity> repo) => _repo = repo;

    public Task<int> Handle(CountCommentsQuery request, CancellationToken ct) => _repo.CountAsync(ct: ct);
}