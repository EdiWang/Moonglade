﻿using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public enum CountType
{
    Public,
    Category,
    Tag,
    Featured
}

public record CountPostQuery(CountType CountType, Guid? CatId = null, int? TagId = null) : IRequest<int>;

public class CountPostQueryHandler(
    MoongladeRepository<PostEntity> postRepo,
    MoongladeRepository<PostTagEntity> postTagRepo,
    MoongladeRepository<PostCategoryEntity> postCatRepo)
    : IRequestHandler<CountPostQuery, int>
{
    public async Task<int> Handle(CountPostQuery request, CancellationToken ct)
    {
        int count = 0;

        switch (request.CountType)
        {
            case CountType.Public:
                count = await postRepo.CountAsync(new PostByStatusSpec(PostStatus.Published), ct);
                break;

            case CountType.Category:
                if (request.CatId == null) throw new ArgumentNullException(nameof(request.CatId));
                count = await postCatRepo.CountAsync(c => c.CategoryId == request.CatId.Value
                                                           && c.Post.PostStatus == PostStatusConstants.Published
                                                           && !c.Post.IsDeleted, ct);
                break;

            case CountType.Tag:
                if (request.TagId == null) throw new ArgumentNullException(nameof(request.TagId));
                count = await postTagRepo.CountAsync(p => p.TagId == request.TagId.Value && p.Post.PostStatus == PostStatusConstants.Published && !p.Post.IsDeleted, ct);
                break;

            case CountType.Featured:
                count = await postRepo.CountAsync(new FeaturedPostSpec(), ct);
                break;
        }

        return count;
    }
}