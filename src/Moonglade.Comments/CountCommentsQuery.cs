using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Comments;

public record CountCommentsQuery : IRequest<int>;

public class CountCommentsQueryHandler : RequestHandler<CountCommentsQuery, int>
{
    private readonly IRepository<CommentEntity> _commentRepo;

    public CountCommentsQueryHandler(IRepository<CommentEntity> commentRepo) => _commentRepo = commentRepo;

    protected override int Handle(CountCommentsQuery request) => _commentRepo.Count(c => true);
}