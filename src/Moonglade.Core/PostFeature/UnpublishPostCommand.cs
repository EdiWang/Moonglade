﻿using Edi.CacheAside.InMemory;
using Moonglade.Data;

namespace Moonglade.Core.PostFeature;

public record UnpublishPostCommand(Guid Id) : IRequest;

public class UnpublishPostCommandHandler(MoongladeRepository<PostEntity> repo, ICacheAside cache) : IRequestHandler<UnpublishPostCommand>
{
    public async Task Handle(UnpublishPostCommand request, CancellationToken ct)
    {
        var post = await repo.GetByIdAsync(request.Id, ct);
        if (null == post) return;

        post.IsPublished = false;
        post.PubDateUtc = null;
        post.RouteLink = null;
        post.LastModifiedUtc = DateTime.UtcNow;

        await repo.UpdateAsync(post, ct);

        cache.Remove(BlogCachePartition.Post.ToString(), request.Id.ToString());
    }
}