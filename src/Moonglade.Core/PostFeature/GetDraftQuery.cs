﻿using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record GetDraftQuery(Guid Id) : IQuery<PostEntity>;

public class GetDraftQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<GetDraftQuery, PostEntity>
{
    public Task<PostEntity> HandleAsync(GetDraftQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = repo.FirstOrDefaultAsync(spec, ct);
        return post;
    }
}