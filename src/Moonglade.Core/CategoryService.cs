using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface ICategoryService
    {
        Task CreateAsync(string displayName, string routeName, string note = null);
        Task<OperationCode> UpdateAsync(Guid id, string displayName, string routeName, string note = null);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IRepository<CategoryEntity> _catRepo;
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;

        public CategoryService(
            IRepository<CategoryEntity> catRepo,
            IBlogAudit audit,
            IBlogCache cache)
        {
            _catRepo = catRepo;
            _audit = audit;
            _cache = cache;
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