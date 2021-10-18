using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Comments;

public class CountCommentsQuery : IRequest<int>
{
}

public class CountCommentsQueryHandler : IRequestHandler<CountCommentsQuery, int>
{
    private readonly IRepository<CommentEntity> _commentRepo;

    public CountCommentsQueryHandler(IRepository<CommentEntity> commentRepo)
    {
        _commentRepo = commentRepo;
    }

    public Task<int> Handle(CountCommentsQuery request, CancellationToken cancellationToken)
    {
        var count = _commentRepo.Count(c => true);
        return Task.FromResult(count);
    }
}