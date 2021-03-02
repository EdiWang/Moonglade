using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core
{
    public interface ISearchService
    {
        Task<IReadOnlyList<PostDigest>> SearchAsync(string keyword);
    }

    public class SearchService : ISearchService
    {
        private readonly IRepository<PostEntity> _postRepo;

        public SearchService(IRepository<PostEntity> postRepo)
        {
            _postRepo = postRepo;
        }

        public async Task<IReadOnlyList<PostDigest>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                throw new ArgumentNullException(keyword);
            }

            var postList = SearchByKeyword(keyword);

            var resultList = await postList.Select(p => new PostDigest
            {
                Title = p.Title,
                Slug = p.Slug,
                ContentAbstract = p.ContentAbstract,
                PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
                Tags = p.Tags.Select(pt => new Tag
                {
                    NormalizedName = pt.NormalizedName,
                    DisplayName = pt.DisplayName
                })
            }).ToListAsync();

            return resultList;
        }


        private IQueryable<PostEntity> SearchByKeyword(string keyword)
        {
            var query = _postRepo.GetAsQueryable()
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
}
