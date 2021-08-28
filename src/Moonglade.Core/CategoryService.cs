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