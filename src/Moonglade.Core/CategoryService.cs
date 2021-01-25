using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<Category>> GetAllAsync();
        Task<Category> GetAsync(string categoryName);
        Task<Category> GetAsync(Guid id);
        Task CreateAsync(UpdateCatRequest request);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(Guid id, UpdateCatRequest request);
    }

    public class CategoryService : ICategoryService
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

        public async Task CreateAsync(UpdateCatRequest request)
        {
            var exists = _catRepo.Any(c => c.RouteName == request.RouteName);
            if (exists) return;

            var category = new CategoryEntity
            {
                Id = Guid.NewGuid(),
                RouteName = request.RouteName.Trim(),
                Note = request.Note.Trim(),
                DisplayName = request.DisplayName.Trim()
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

        public async Task UpdateAsync(Guid id, UpdateCatRequest request)
        {
            var cat = await _catRepo.GetAsync(id);
            if (cat is null) return;

            cat.RouteName = request.RouteName.Trim();
            cat.DisplayName = request.DisplayName.Trim();
            cat.Note = request.Note.Trim();

            await _catRepo.UpdateAsync(cat);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddAuditEntry(EventType.Content, AuditEventId.CategoryUpdated, $"Category '{id}' updated.");
        }
    }
}