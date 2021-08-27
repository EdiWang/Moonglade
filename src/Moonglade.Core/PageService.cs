using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface IBlogPageService
    {
        Task<BlogPage> GetAsync(Guid pageId);
        Task<IReadOnlyList<BlogPage>> GetAsync(int top);
        Task<IReadOnlyList<PageSegment>> ListSegmentAsync();
        Task<Guid> CreateAsync(PageEditModel request);
        Task<Guid> UpdateAsync(Guid id, PageEditModel request);
    }

    public class BlogPageService : IBlogPageService
    {
        private readonly IRepository<PageEntity> _pageRepo;
        private readonly IBlogAudit _audit;

        public BlogPageService(
            IRepository<PageEntity> pageRepo,
            IBlogAudit audit)
        {
            _pageRepo = pageRepo;
            _audit = audit;
        }

        public async Task<BlogPage> GetAsync(Guid pageId)
        {
            var entity = await _pageRepo.GetAsync(pageId);
            if (entity == null) return null;

            var item = new BlogPage(entity);
            return item;
        }

        public async Task<IReadOnlyList<BlogPage>> GetAsync(int top)
        {
            if (top <= 0) throw new ArgumentOutOfRangeException(nameof(top));

            var pages = await _pageRepo.GetAsync(new PageSpec(top));
            var list = pages.Select(p => new BlogPage(p)).ToList();
            return list;
        }

        public Task<IReadOnlyList<PageSegment>> ListSegmentAsync()
        {
            return _pageRepo.SelectAsync(page => new PageSegment
            {
                Id = page.Id,
                CreateTimeUtc = page.CreateTimeUtc,
                Slug = page.Slug,
                Title = page.Title,
                IsPublished = page.IsPublished
            });
        }

        public async Task<Guid> CreateAsync(PageEditModel request)
        {
            var uid = Guid.NewGuid();
            var page = new PageEntity
            {
                Id = uid,
                Title = request.Title.Trim(),
                Slug = request.Slug.ToLower().Trim(),
                MetaDescription = request.MetaDescription,
                CreateTimeUtc = DateTime.UtcNow,
                HtmlContent = request.RawHtmlContent,
                CssContent = request.CssContent,
                HideSidebar = request.HideSidebar,
                IsPublished = request.IsPublished
            };

            await _pageRepo.AddAsync(page);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PageCreated, $"Page '{page.Id}' created.");

            return uid;
        }

        public async Task<Guid> UpdateAsync(Guid id, PageEditModel request)
        {
            var page = await _pageRepo.GetAsync(id);
            if (page is null)
            {
                throw new InvalidOperationException($"PageEntity with Id '{id}' not found.");
            }

            page.Title = request.Title.Trim();
            page.Slug = request.Slug.ToLower().Trim();
            page.MetaDescription = request.MetaDescription;
            page.HtmlContent = request.RawHtmlContent;
            page.CssContent = request.CssContent;
            page.HideSidebar = request.HideSidebar;
            page.UpdateTimeUtc = DateTime.UtcNow;
            page.IsPublished = request.IsPublished;

            await _pageRepo.UpdateAsync(page);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PageUpdated, $"Page '{id}' updated.");

            return page.Id;
        }

        public static string RemoveScriptTagFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var regex = new Regex("\\<script(.+?)\\</script\\>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var result = regex.Replace(html, string.Empty);
            return result;
        }
    }
}
