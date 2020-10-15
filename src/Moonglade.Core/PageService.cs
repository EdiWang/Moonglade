using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class PageService : BlogService
    {
        private readonly IRepository<PageEntity> _customPageRepository;
        private readonly IBlogAudit _blogAudit;

        public PageService(
            ILogger<PageService> logger,
            IOptions<AppSettings> settings,
            IRepository<PageEntity> customPageRepository,
            IBlogAudit blogAudit) : base(logger, settings)
        {
            _customPageRepository = customPageRepository;
            _blogAudit = blogAudit;
        }

        public async Task<Page> GetAsync(Guid pageId)
        {
            var entity = await _customPageRepository.GetAsync(pageId);
            var item = EntityToPage(entity);
            return item;
        }

        public async Task<Page> GetAsync(string slug)
        {
            var loweredRouteName = slug.ToLower();
            var entity = await _customPageRepository.GetAsync(p => p.Slug == loweredRouteName);
            var item = EntityToPage(entity);
            return item;
        }

        public Task<IReadOnlyList<PageSegment>> ListSegmentAsync()
        {
            return _customPageRepository.SelectAsync(page => new PageSegment
            {
                Id = page.Id,
                CreateOnUtc = page.CreateOnUtc,
                RouteName = page.Slug,
                Title = page.Title,
                IsPublished = page.IsPublished
            });
        }

        public async Task<Guid> CreateAsync(CreatePageRequest request)
        {
            var uid = Guid.NewGuid();
            var customPage = new PageEntity
            {
                Id = uid,
                Title = request.Title.Trim(),
                Slug = request.Slug.ToLower().Trim(),
                MetaDescription = request.MetaDescription,
                CreateOnUtc = DateTime.UtcNow,
                HtmlContent = request.HtmlContent,
                CssContent = request.CssContent,
                HideSidebar = request.HideSidebar,
                IsPublished = request.IsPublished
            };

            await _customPageRepository.AddAsync(customPage);
            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.PageCreated, $"Page '{customPage.Id}' created.");

            return uid;
        }

        public async Task<Guid> UpdateAsync(EditPageRequest request)
        {
            var page = await _customPageRepository.GetAsync(request.Id);
            if (null == page)
            {
                throw new InvalidOperationException($"CustomPageEntity with Id '{request.Id}' not found.");
            }

            page.Title = request.Title.Trim();
            page.Slug = request.Slug.ToLower().Trim();
            page.MetaDescription = request.MetaDescription;
            page.HtmlContent = request.HtmlContent;
            page.CssContent = request.CssContent;
            page.HideSidebar = request.HideSidebar;
            page.UpdatedOnUtc = DateTime.UtcNow;
            page.IsPublished = request.IsPublished;

            await _customPageRepository.UpdateAsync(page);
            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.PageUpdated, $"Page '{request.Id}' updated.");

            return page.Id;
        }

        public async Task DeleteAsync(Guid pageId)
        {
            var page = await _customPageRepository.GetAsync(pageId);
            if (null == page)
            {
                throw new InvalidOperationException($"CustomPageEntity with Id '{pageId}' not found.");
            }

            await _customPageRepository.DeleteAsync(pageId);
            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.PageDeleted, $"Page '{pageId}' deleted.");
        }

        public static string RemoveWhiteSpaceFromStylesheets(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            body = Regex.Replace(body, "[a-zA-Z]+#", "#");
            body = Regex.Replace(body, @"[\n\r]+\s*", string.Empty);
            body = Regex.Replace(body, @"\s+", " ");
            body = Regex.Replace(body, @"\s?([:,;{}])\s?", "$1");
            body = body.Replace(";}", "}");
            body = Regex.Replace(body, @"([\s:]0)(px|pt|%|em)", "$1");
            // Remove comments from CSS
            body = Regex.Replace(body, @"/\*[\d\D]*?\*/", string.Empty);
            return body;
        }

        public static string RemoveScriptTagFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var regex = new Regex("\\<script(.+?)\\</script\\>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var result = regex.Replace(html, string.Empty);
            return result;
        }

        private static Page EntityToPage(PageEntity entity)
        {
            if (null == entity)
            {
                return null;
            }

            return new Page
            {
                Id = entity.Id,
                Title = entity.Title.Trim(),
                CreateOnUtc = entity.CreateOnUtc,
                CssContent = entity.CssContent,
                RawHtmlContent = entity.HtmlContent,
                HideSidebar = entity.HideSidebar,
                Slug = entity.Slug.Trim().ToLower(),
                MetaDescription = entity.MetaDescription?.Trim(),
                UpdatedOnUtc = entity.UpdatedOnUtc,
                IsPublished = entity.IsPublished
            };
        }
    }
}
