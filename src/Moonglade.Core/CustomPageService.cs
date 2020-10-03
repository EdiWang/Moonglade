using System;
using System.Collections.Generic;
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
    public class CustomPageService : BlogService
    {
        private readonly IRepository<CustomPageEntity> _customPageRepository;
        private readonly IBlogAudit _blogAudit;

        public CustomPageService(
            ILogger<CustomPageService> logger,
            IOptions<AppSettings> settings,
            IRepository<CustomPageEntity> customPageRepository,
            IBlogAudit blogAudit) : base(logger, settings)
        {
            _customPageRepository = customPageRepository;
            _blogAudit = blogAudit;
        }

        public async Task<CustomPage> GetAsync(Guid pageId)
        {
            var entity = await _customPageRepository.GetAsync(pageId);
            var item = EntityToCustomPage(entity);
            return item;
        }

        public async Task<CustomPage> GetAsync(string slug)
        {
            var loweredRouteName = slug.ToLower();
            var entity = await _customPageRepository.GetAsync(p => p.Slug == loweredRouteName);
            var item = EntityToCustomPage(entity);
            return item;
        }

        public Task<IReadOnlyList<CustomPageSegment>> ListSegmentAsync()
        {
            return _customPageRepository.SelectAsync(page => new CustomPageSegment
            {
                Id = page.Id,
                CreateOnUtc = page.CreateOnUtc,
                RouteName = page.Slug,
                Title = page.Title,
                IsPublished = page.IsPublished
            });
        }

        public async Task<Guid> CreateAsync(CreateCustomPageRequest request)
        {
            var uid = Guid.NewGuid();
            var customPage = new CustomPageEntity
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

        public async Task<Guid> UpdateAsync(EditCustomPageRequest request)
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

        private static CustomPage EntityToCustomPage(CustomPageEntity entity)
        {
            if (null == entity)
            {
                return null;
            }

            return new CustomPage
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
