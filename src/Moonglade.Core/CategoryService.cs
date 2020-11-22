using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class CategoryService : BlogService
    {
        private readonly IRepository<CategoryEntity> _catRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;

        public CategoryService(
            IRepository<CategoryEntity> catRepo,
            IRepository<PostCategoryEntity> postCatRepo,
            IBlogAudit audit,
            IBlogCache cache)
        {
            _catRepo = catRepo;
            _postCatRepo = postCatRepo;
            _audit = audit;
            _cache = cache;
        }

        public Task<IReadOnlyList<Category>> GetAllAsync()
        {
            return _cache.GetOrCreateAsync(CacheDivision.General, "allcats", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                var list = await _catRepo.SelectAsync(c => new Category
                {
                    Id = c.Id,
                    DisplayName = c.DisplayName,
                    RouteName = c.RouteName,
                    Note = c.Note
                });
                return list;
            });
        }

        public Task<Category> GetAsync(string categoryName)
        {
            return _catRepo.SelectFirstOrDefaultAsync(
                new CategorySpec(categoryName), category =>
                    new Category
                    {
                        DisplayName = category.DisplayName,
                        Id = category.Id,
                        RouteName = category.RouteName,
                        Note = category.Note
                    });
        }

        public Task<Category> GetAsync(Guid id)
        {
            return _catRepo.SelectFirstOrDefaultAsync(
                new CategorySpec(id), category =>
                    new Category
                    {
                        DisplayName = category.DisplayName,
                        Id = category.Id,
                        RouteName = category.RouteName,
                        Note = category.Note
                    });
        }

        public async Task CreateAsync(CreateCategoryRequest createRequest)
        {
            var exists = _catRepo.Any(c => c.RouteName == createRequest.RouteName);
            if (exists) return;

            var category = new CategoryEntity
            {
                Id = Guid.NewGuid(),
                RouteName = createRequest.RouteName.Trim(),
                Note = createRequest.Note.Trim(),
                DisplayName = createRequest.DisplayName.Trim()
            };

            await _catRepo.AddAsync(category);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddAuditEntry(EventType.Content, AuditEventId.CategoryCreated, $"Category '{category.RouteName}' created");
        }

        public async Task DeleteAsync(Guid id)
        {
            var exists = _catRepo.Any(c => c.Id == id);
            if (!exists) return;

            var pcs = await _postCatRepo.GetAsync(pc => pc.CategoryId == id);
            if (pcs is not null) await _postCatRepo.DeleteAsync(pcs);

            _catRepo.Delete(id);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddAuditEntry(EventType.Content, AuditEventId.CategoryDeleted, $"Category '{id}' deleted.");
        }

        public async Task UpdateAsync(EditCategoryRequest editRequest)
        {
            var cat = await _catRepo.GetAsync(editRequest.Id);
            if (cat is null) return;

            cat.RouteName = editRequest.RouteName.Trim();
            cat.DisplayName = editRequest.DisplayName.Trim();
            cat.Note = editRequest.Note.Trim();

            await _catRepo.UpdateAsync(cat);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddAuditEntry(EventType.Content, AuditEventId.CategoryUpdated, $"Category '{editRequest.Id}' updated.");
        }
    }
}