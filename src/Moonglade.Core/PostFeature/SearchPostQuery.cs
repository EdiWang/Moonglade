using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Moonglade.Core.PostFeature;

public record SearchPostQuery(string Keyword) : IRequest<IReadOnlyList<PostDigest>>;

public class SearchPostQueryHandler : IRequestHandler<SearchPostQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _repo;
    public SearchPostQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public async Task<IReadOnlyList<PostDigest>> Handle(SearchPostQuery request, CancellationToken ct)
    {
        if (null == request || string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ArgumentNullException(request?.Keyword);
        }

        var postList = SearchByKeyword(request.Keyword);
        var resultList = await postList.Select(PostDigest.EntitySelector).ToListAsync(ct);

        return resultList;
    }

    private IQueryable<PostEntity> SearchByKeyword(string keyword)
    {
        var query = _repo.AsQueryable()
            .Where(p => !p.IsDeleted && p.IsPublished).AsNoTracking();

        var str = Regex.Replace(keyword, @"\s+", " ");
        var rst = str.Split(' ');
        if (rst.Length > 1)
        {
            // keyword: "dot  net rocks"
            // search for post where Title containing "dot && net && rocks"
            var result = rst.Aggregate(query, (current, s) => current.Where(p => p.Title.Contains(s)));
            return result;
        }
        else
        {
            // keyword: "dotnetrocks"
            var k = rst.First();
            var result = query.Where(p => p.Title.Contains(k) ||
                                          p.Tags.Select(t => t.DisplayName).Contains(k));
            return result;
        }
    }
}