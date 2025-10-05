﻿using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record SearchPostQuery(string Keyword) : IQuery<List<PostDigest>>;

public class SearchPostQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<SearchPostQuery, List<PostDigest>>
{
    public async Task<List<PostDigest>> HandleAsync(SearchPostQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ArgumentException("Keyword must not be null or whitespace.", nameof(request.Keyword));
        }

        var spec = new SearchPostsSpec(request.Keyword);
        var results = await repo.ListAsync(spec, ct);

        return results;
    }
}
