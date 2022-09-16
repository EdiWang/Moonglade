using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record GetCommentsQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<CommentDetailedItem>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDetailedItem>>
{
    private readonly IRepository<CommentEntity> _commentRepo;
    public GetCommentsQueryHandler(IRepository<CommentEntity> commentRepo) => _commentRepo = commentRepo;

    public Task<IReadOnlyList<CommentDetailedItem>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageSize), $"{nameof(request.PageSize)} can not be less than 1.");
        }

        var spec = new CommentSpec(request.PageSize, request.PageIndex);
        var comments = _commentRepo.SelectAsync(spec, CommentDetailedItem.EntitySelector);

        return comments;
    }
}