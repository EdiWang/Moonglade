using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Comments;

public record CountCommentsQuery : IRequest<int>;

public class CountCommentsQueryHandler : RequestHandler<CountCommentsQuery, int>
{
    private readonly IRepository<CommentEntity> _repo;

    public CountCommentsQueryHandler(IRepository<CommentEntity> repo) => _repo = repo;

    protected override int Handle(CountCommentsQuery request) => _repo.Count(c => true);
}