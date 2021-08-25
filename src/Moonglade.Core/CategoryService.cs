using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<Category>> GetAllAsync();
        Task<Category> GetAsync(string routeName);
        Task<Category> GetAsync(Guid id);
        Task CreateAsync(string displayName, string routeName, string note = null);
        Task<OperationCode> DeleteAsync(Guid id);
        Task<OperationCode> UpdateAsync(Guid id, string displayName, string routeName, string note = null);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IRepository<CategoryEntity> _catRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;

        private readonly Expression<Func<CategoryEntity, Category>> _categorySelector = c => new()
        {
            Id = c.Id,
            DisplayName = c.DisplayName,
            RouteName = c.RouteName,
            Note = c.Note
        };

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
                var list = await _catRepo.SelectAsync(_categorySelector);
                return list;
            });
        }

        public Task<Category> GetAsync(string routeName)
        {
            return _catRepo.SelectFirstOrDefaultAsync(new CategorySpec(routeName), _categorySelector);
        }

        public Task<Category> GetAsync(Guid id)
        {
            return _catRepo.SelectFirstOrDefaultAsync(new CategorySpec(id), _categorySelector);
        }

        public async Task CreateAsync(string displayName, string routeName, string note = null)
        {
            var exists = _catRepo.Any(c => c.RouteName == routeName);
            if (exists) return;

            var category = new CategoryEntity
            {
                Id = Guid.NewGuid(),
                RouteName = routeName.Trim(),
                Note = note?.Trim(),
                DisplayName = displayName.Trim()
            };

            await _catRepo.AddAsync(category);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddEntry(BlogEventType.Content, BlogEventId.CategoryCreated, $"Category '{category.RouteName}' created");
        }

        public async Task<OperationCode> DeleteAsync(Guid id)
        {
            var exists = _catRepo.Any(c => c.Id == id);
            if (!exists) return OperationCode.ObjectNotFound;

            var pcs = await _postCatRepo.GetAsync(pc => pc.CategoryId == id);
            if (pcs is not null) await _postCatRepo.DeleteAsync(pcs);

            await _catRepo.DeleteAsync(id);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddEntry(BlogEventType.Content, BlogEventId.CategoryDeleted, $"Category '{id}' deleted.");
            return OperationCode.Done;
        }

        public async Task<OperationCode> UpdateAsync(Guid id, string displayName, string routeName, string note = null)
        {
            var cat = await _catRepo.GetAsync(id);
            if (cat is null) return OperationCode.ObjectNotFound;

            cat.RouteName = routeName.Trim();
            cat.DisplayName = displayName.Trim();
            cat.Note = note?.Trim();

            await _catRepo.UpdateAsync(cat);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddEntry(BlogEventType.Content, BlogEventId.CategoryUpdated, $"Category '{id}' updated.");
            return OperationCode.Done;
        }
    }
}