using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Comments
{
    public class GetCommentsQuery : IRequest<IReadOnlyList<CommentDetailedItem>>
    {
        public GetCommentsQuery(int pageSize, int pageIndex)
        {
            PageSize = pageSize;
            PageIndex = pageIndex;
        }

        public int PageSize { get; set; }
        public int PageIndex { get; set; }
    }

    public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDetailedItem>>
    {
        private readonly IRepository<CommentEntity> _commentRepo;

        public GetCommentsQueryHandler(IRepository<CommentEntity> commentRepo)
        {
            _commentRepo = commentRepo;
        }

        public Task<IReadOnlyList<CommentDetailedItem>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
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
}
