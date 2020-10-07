using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Core.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class CategoryService : BlogService
    {
        private readonly IRepository<CategoryEntity> _categoryRepository;
        private readonly IRepository<PostCategoryEntity> _postCategoryRepository;
        private readonly IBlogAudit _blogAudit;
        private readonly IBlogCache _cache;

        public CategoryService(ILogger<CategoryService> logger,
            IRepository<CategoryEntity> categoryRepository,
            IRepository<PostCategoryEntity> postCategoryRepository,
            IBlogAudit blogAudit, 
            IBlogCache cache) : base(logger)
        {
            _categoryRepository = categoryRepository;
            _postCategoryRepository = postCategoryRepository;
            _blogAudit = blogAudit;
            _cache = cache;
        }

        public Task<IReadOnlyList<Category>> GetAllAsync()
        {
            return _cache.GetOrCreateAsync(CacheDivision.General, "allcats", async entry =>
            {
                var list = await _categoryRepository.SelectAsync(c => new Category
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
            return _categoryRepository.SelectFirstOrDefaultAsync(
                new CategorySpec(categoryName), category =>
                    new Category
                    {
                        DisplayName = category.DisplayName,
                        Id = category.Id,
                        RouteName = category.RouteName,
                        Note = category.Note
                    });
        }

        public Task<Category> GetAsync(Guid categoryId)
        {
            return _categoryRepository.SelectFirstOrDefaultAsync(
                new CategorySpec(categoryId), category =>
                    new Category
                    {
                        DisplayName = category.DisplayName,
                        Id = category.Id,
                        RouteName = category.RouteName,
                        Note = category.Note
                    });
        }

        public async Task CreateAsync(CreateCategoryRequest createCategoryRequest)
        {
            var exists = _categoryRepository.Any(c => c.RouteName == createCategoryRequest.RouteName);
            if (exists) return;

            var category = new CategoryEntity
            {
                Id = Guid.NewGuid(),
                RouteName = createCategoryRequest.RouteName.Trim(),
                Note = createCategoryRequest.Note.Trim(),
                DisplayName = createCategoryRequest.DisplayName.Trim()
            };

            await _categoryRepository.AddAsync(category);
            _cache.Remove(CacheDivision.General, "allcats");

            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.CategoryCreated, $"Category '{category.RouteName}' created");
        }

        public async Task DeleteAsync(Guid id)
        {
            var exists = _categoryRepository.Any(c => c.Id == id);
            if (!exists) return;

            var pcs = await _postCategoryRepository.GetAsync(pc => pc.CategoryId == id);
            if (null != pcs)
            {
                await _postCategoryRepository.DeleteAsync(pcs);
            }

            _categoryRepository.Delete(id);
            _cache.Remove(CacheDivision.General, "allcats");

            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.CategoryDeleted, $"Category '{id}' deleted.");
        }

        public async Task UpdateAsync(EditCategoryRequest editCategoryRequest)
        {
            var cat = await _categoryRepository.GetAsync(editCategoryRequest.Id);
            if (null == cat) return;

            cat.RouteName = editCategoryRequest.RouteName.Trim();
            cat.DisplayName = editCategoryRequest.DisplayName.Trim();
            cat.Note = editCategoryRequest.Note.Trim();

            await _categoryRepository.UpdateAsync(cat);
            _cache.Remove(CacheDivision.General, "allcats");

            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.CategoryUpdated, $"Category '{editCategoryRequest.Id}' updated.");
        }
    }
}