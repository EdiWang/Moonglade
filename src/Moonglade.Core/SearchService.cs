using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class SearchService : BlogService
    {
        private readonly IRepository<PostEntity> _postRepository;

        public SearchService(
            ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepository) : base(logger, settings)
        {
            _postRepository = postRepository;
        }

        public Task<Response<IReadOnlyList<PostListEntry>>> SearchAsync(string keyword)
        {
            return TryExecuteAsync<IReadOnlyList<PostListEntry>>(async () =>
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    throw new ArgumentNullException(keyword);
                }

                var postList = SearchByKeyword(keyword);

                var resultList = await postList.Select(p => new PostListEntry
                {
                    Title = p.Title,
                    Slug = p.Slug,
                    ContentAbstract = p.ContentAbstract,
                    PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
                    Tags = p.PostTag.Select(pt => new Tag
                    {
                        NormalizedName = pt.Tag.NormalizedName,
                        DisplayName = pt.Tag.DisplayName
                    }).ToList()
                }).ToListAsync();

                return new SuccessResponse<IReadOnlyList<PostListEntry>>(resultList);
            }, keyParameter: keyword);
        }

        private IQueryable<PostEntity> SearchByKeyword(string keyword)
        {
            var query = _postRepository.GetAsQueryable()
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
                                              p.PostTag.Select(pt => pt.Tag).Select(t => t.DisplayName).Contains(k));
                return result;
            }
        }
    }
}
