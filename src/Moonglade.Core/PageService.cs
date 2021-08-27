using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface IBlogPageService
    {
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
