using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.PostFeature
{
    public class ListPostSegmentQuery : IRequest<(IReadOnlyList<PostSegment> Posts, int TotalRows)>
    {
        public ListPostSegmentQuery(PostStatus postStatus, int offset, int pageSize, string keyword = null)
        {
            PostStatus = postStatus;
            Offset = offset;
            PageSize = pageSize;
            Keyword = keyword;
        }

        public PostStatus PostStatus { get; set; }

        public int Offset { get; set; }

        public int PageSize { get; set; }

        public string Keyword { get; set; }
    }

    public class ListPostSegmentQueryHandler : IRequestHandler<ListPostSegmentQuery, (IReadOnlyList<PostSegment> Posts, int TotalRows)>
    {
        private readonly IRepository<PostEntity> _postRepo;

        public ListPostSegmentQueryHandler(IRepository<PostEntity> postRepo)
        {
            _postRepo = postRepo;
        }

        public async Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> Handle(ListPostSegmentQuery request, CancellationToken cancellationToken)
        {
            if (request.PageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(request.PageSize),
                    $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
            }
            if (request.Offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.Offset),
                    $"{nameof(request.Offset)} can not be less than 0, current value: {request.Offset}.");
            }

            var spec = new PostPagingSpec(request.PostStatus, request.Keyword, request.PageSize, request.Offset);
            var posts = await _postRepo.SelectAsync(spec, PostSegment.EntitySelector);

            Expression<Func<PostEntity, bool>> countExp = p => null == request.Keyword || p.Title.Contains(request.Keyword);

            switch (request.PostStatus)
            {
                case PostStatus.Draft:
                    countExp.AndAlso(p => !p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Published:
                    countExp.AndAlso(p => p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Deleted:
                    countExp.AndAlso(p => p.IsDeleted);
                    break;
                case PostStatus.Default:
                    countExp.AndAlso(p => true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.PostStatus), request.PostStatus, null);
            }

            var totalRows = _postRepo.Count(countExp);
            return (posts, totalRows);
        }
    }
}
