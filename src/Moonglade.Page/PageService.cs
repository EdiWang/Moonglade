using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Moonglade.Page
{
    public interface IBlogPageService
    {
        Task<BlogPage> GetAsync(Guid pageId);
        Task<BlogPage> GetAsync(string slug);
        Task<IReadOnlyList<BlogPage>> GetAsync(int top);
        Task<IReadOnlyList<PageSegment>> ListSegment();
        Task<Guid> CreateAsync(UpdatePageRequest request);
        Task<Guid> UpdateAsync(Guid id, UpdatePageRequest request);
        Task DeleteAsync(Guid pageId);
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
            var item = EntityToPage(entity);
            return item;
        }

        public async Task<BlogPage> GetAsync(string slug)
        {
            var lower = slug.ToLower();
            var entity = await _pageRepo.GetAsync(p => p.Slug == lower);
            var item = EntityToPage(entity);
            return item;
        }

        public async Task<IReadOnlyList<BlogPage>> GetAsync(int top)
        {
            if (top <= 0) throw new ArgumentOutOfRangeException(nameof(top));

            var pages = await _pageRepo.GetAsync(new PageSpec(top));
            var list = pages.Select(EntityToPage).ToList();
            return list;
        }

        public Task<IReadOnlyList<PageSegment>> ListSegment()
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

        public async Task<Guid> CreateAsync(UpdatePageRequest request)
        {
            var uid = Guid.NewGuid();
            var page = new PageEntity
            {
                Id = uid,
                Title = request.Title.Trim(),
                Slug = request.Slug.ToLower().Trim(),
                MetaDescription = request.MetaDescription,
                CreateTimeUtc = DateTime.UtcNow,
                HtmlContent = request.HtmlContent,
                CssContent = request.CssContent,
                HideSidebar = request.HideSidebar,
                IsPublished = request.IsPublished
            };

            await _pageRepo.AddAsync(page);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PageCreated, $"Page '{page.Id}' created.");

            return uid;
        }

        public async Task<Guid> UpdateAsync(Guid id, UpdatePageRequest request)
        {
            var page = await _pageRepo.GetAsync(id);
            if (page is null)
            {
                throw new InvalidOperationException($"PageEntity with Id '{id}' not found.");
            }

            page.Title = request.Title.Trim();
            page.Slug = request.Slug.ToLower().Trim();
            page.MetaDescription = request.MetaDescription;
            page.HtmlContent = request.HtmlContent;
            page.CssContent = request.CssContent;
            page.HideSidebar = request.HideSidebar;
            page.UpdateTimeUtc = DateTime.UtcNow;
            page.IsPublished = request.IsPublished;

            await _pageRepo.UpdateAsync(page);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PageUpdated, $"Page '{id}' updated.");

            return page.Id;
        }

        public async Task DeleteAsync(Guid pageId)
        {
            var page = await _pageRepo.GetAsync(pageId);
            if (page is null)
            {
                throw new InvalidOperationException($"CustomPageEntity with Id '{pageId}' not found.");
            }

            await _pageRepo.DeleteAsync(pageId);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PageDeleted, $"Page '{pageId}' deleted.");
        }

        public static string RemoveScriptTagFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var regex = new Regex("\\<script(.+?)\\</script\\>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var result = regex.Replace(html, string.Empty);
            return result;
        }

        private static BlogPage EntityToPage(PageEntity entity)
        {
            if (entity is null) return null;

            return new()
            {
                Id = entity.Id,
                Title = entity.Title.Trim(),
                CreateTimeUtc = entity.CreateTimeUtc,
                CssContent = entity.CssContent,
                RawHtmlContent = entity.HtmlContent,
                HideSidebar = entity.HideSidebar,
                Slug = entity.Slug.Trim().ToLower(),
                MetaDescription = entity.MetaDescription?.Trim(),
                UpdateTimeUtc = entity.UpdateTimeUtc,
                IsPublished = entity.IsPublished
            };
        }
    }
}
