using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class CustomPageService : MoongladeService
    {
        private readonly IRepository<CustomPageEntity> _customPageRepository;
        private readonly IMoongladeAudit _moongladeAudit;

        public CustomPageService(
            ILogger<CustomPageService> logger,
            IOptions<AppSettings> settings,
            IRepository<CustomPageEntity> customPageRepository,
            IMoongladeAudit moongladeAudit) : base(logger, settings)
        {
            _customPageRepository = customPageRepository;
            _moongladeAudit = moongladeAudit;
        }

        public Task<Response<CustomPage>> GetPageAsync(Guid pageId)
        {
            return TryExecuteAsync<CustomPage>(async () =>
            {
                var entity = await _customPageRepository.GetAsync(pageId);
                var item = EntityToCustomPage(entity);
                return new SuccessResponse<CustomPage>(item);
            });
        }

        public Task<Response<CustomPage>> GetPageAsync(string slug)
        {
            return TryExecuteAsync<CustomPage>(async () =>
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    throw new ArgumentNullException(nameof(slug));
                }

                var loweredRouteName = slug.ToLower();
                var entity = await _customPageRepository.GetAsync(p => p.Slug == loweredRouteName);
                var item = EntityToCustomPage(entity);
                return new SuccessResponse<CustomPage>(item);
            });
        }

        public Task<Response<IReadOnlyList<CustomPageMetaData>>> GetPagesMetaAsync()
        {
            return TryExecuteAsync<IReadOnlyList<CustomPageMetaData>>(async () =>
            {
                var list = await _customPageRepository.SelectAsync(page => new CustomPageMetaData
                {
                    Id = page.Id,
                    CreateOnUtc = page.CreateOnUtc,
                    RouteName = page.Slug,
                    Title = page.Title
                });

                return new SuccessResponse<IReadOnlyList<CustomPageMetaData>>(list);
            });
        }

        public Task<Response<Guid>> CreatePageAsync(CreateCustomPageRequest request)
        {
            return TryExecuteAsync<Guid>(async () =>
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
                    HideSidebar = request.HideSidebar
                };

                await _customPageRepository.AddAsync(customPage);
                await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.PageCreated, $"Page '{customPage.Id}' created.");

                return new SuccessResponse<Guid>(uid);
            });
        }

        public Task<Response<Guid>> EditPageAsync(EditCustomPageRequest request)
        {
            return TryExecuteAsync<Guid>(async () =>
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

                await _customPageRepository.UpdateAsync(page);
                await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.PageUpdated, $"Page '{request.Id}' updated.");

                return new SuccessResponse<Guid>(page.Id);
            });
        }

        public Task<Response> DeletePageAsync(Guid pageId)
        {
            return TryExecuteAsync(async () =>
            {
                var page = await _customPageRepository.GetAsync(pageId);
                if (null == page)
                {
                    throw new InvalidOperationException($"CustomPageEntity with Id '{pageId}' not found.");
                }

                await _customPageRepository.DeleteAsync(pageId);
                await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.PageDeleted, $"Page '{pageId}' deleted.");

                return new SuccessResponse();
            });
        }

        private CustomPage EntityToCustomPage(CustomPageEntity entity)
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
                UpdatedOnUtc = entity.UpdatedOnUtc
            };
        }
    }
}
