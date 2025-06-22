﻿using Moonglade.Data;
using Moonglade.Data.Specifications;
using System.Linq.Expressions;

namespace Moonglade.Core.PostFeature;

public class ListPostSegmentQuery(PostStatus postStatus, int offset, int pageSize, string keyword = null)
    : IRequest<(List<PostSegment> Posts, int TotalRows)>
{
    public PostStatus PostStatus { get; set; } = postStatus;

    public int Offset { get; set; } = offset;

    public int PageSize { get; set; } = pageSize;

    public string Keyword { get; set; } = keyword;
}

public class ListPostSegmentQueryHandler(MoongladeRepository<PostEntity> repo) :
    IRequestHandler<ListPostSegmentQuery, (List<PostSegment> Posts, int TotalRows)>
{
    public async Task<(List<PostSegment> Posts, int TotalRows)> Handle(ListPostSegmentQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
        }

        if (request.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.Offset)} can not be less than 0, current value: {request.Offset}.");
        }

        var spec = new PostPagingByStatusSpec(request.PostStatus, request.Keyword, request.PageSize, request.Offset);
        var posts = await repo.SelectAsync(spec, PostSegment.EntitySelector, ct);

        Expression<Func<PostEntity, bool>> countExp = p => null == request.Keyword || p.Title.Contains(request.Keyword);

        switch (request.PostStatus)
        {
            case PostStatus.Draft:
                countExp.AndAlso(p => p.PostStatus == PostStatusConstants.Draft && !p.IsDeleted);
                break;
            case PostStatus.Published:
                countExp.AndAlso(p => p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);
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

        var totalRows = await repo.CountAsync(countExp, ct);
        return (posts, totalRows);
    }
}