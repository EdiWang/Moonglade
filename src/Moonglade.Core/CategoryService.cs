using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Core
{
    public class CategoryService : MoongladeService
    {
        private readonly IRepository<CategoryEntity> _categoryRepository;
        private readonly IRepository<PostCategoryEntity> _postCategoryRepository;
        private readonly IMoongladeAudit _moongladeAudit;

        public CategoryService(ILogger<CategoryService> logger,
            IRepository<CategoryEntity> categoryRepository,
            IRepository<PostCategoryEntity> postCategoryRepository, 
            IMoongladeAudit moongladeAudit) : base(logger)
        {
            _categoryRepository = categoryRepository;
            _postCategoryRepository = postCategoryRepository;
            _moongladeAudit = moongladeAudit;
        }

        public Task<Response<IReadOnlyList<Category>>> GetAllCategoriesAsync()
        {
            return TryExecuteAsync<IReadOnlyList<Category>>(async () =>
            {
                var item = await _categoryRepository.SelectAsync(cat => new Category
                {
                    Id = cat.Id,
                    DisplayName = cat.DisplayName,
                    Name = cat.Title,
                    Note = cat.Note
                });
                return new SuccessResponse<IReadOnlyList<Category>>(item);
            });
        }

        public Task<Response<Category>> GetCategoryAsync(string categoryName)
        {
            return TryExecuteAsync<Category>(async () =>
            {
                var cat = await _categoryRepository.SelectFirstOrDefaultAsync(
                    new CategorySpec(categoryName), category =>
                        new Category
                        {
                            DisplayName = category.DisplayName,
                            Id = category.Id,
                            Name = category.Title,
                            Note = category.Note
                        });

                return new SuccessResponse<Category>(cat);
            }, keyParameter: categoryName);
        }

        public Task<Response<Category>> GetCategoryAsync(Guid categoryId)
        {
            return TryExecuteAsync<Category>(async () =>
            {
                var cat = await _categoryRepository.SelectFirstOrDefaultAsync(
                    new CategorySpec(categoryId), category =>
                        new Category
                        {
                            DisplayName = category.DisplayName,
                            Id = category.Id,
                            Name = category.Title,
                            Note = category.Note
                        });

                return new SuccessResponse<Category>(cat);
            }, keyParameter: categoryId);
        }

        public Task<Response<IReadOnlyList<Category>>> GetCategoriesAsync()
        {
            return TryExecuteAsync<IReadOnlyList<Category>>(async () =>
            {
                var list = await _categoryRepository.SelectAsync(c => new Category
                {
                    Id = c.Id,
                    DisplayName = c.DisplayName,
                    Name = c.Title,
                    Note = c.Note
                });

                return new SuccessResponse<IReadOnlyList<Category>>(list);
            });
        }

        public Task<Response> CreateCategoryAsync(CreateCategoryRequest createCategoryRequest)
        {
            return TryExecuteAsync(async () =>
            {
                var exists = _categoryRepository.Any(c => c.Title == createCategoryRequest.Title);
                if (exists)
                {
                    return new Response { Message = $"CategoryEntity titled '{createCategoryRequest.Title}' already exist." };
                }

                var category = new CategoryEntity
                {
                    Id = Guid.NewGuid(),
                    Title = createCategoryRequest.Title.Trim(),
                    Note = createCategoryRequest.Note.Trim(),
                    DisplayName = createCategoryRequest.DisplayName.Trim()
                };

                await _categoryRepository.AddAsync(category);
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CategoryCreated, $"Category '{category.Title}' created");

                return new SuccessResponse();
            });
        }

        public Task<Response> DeleteAsync(Guid id)
        {
            return TryExecuteAsync(async () =>
            {
                var exists = _categoryRepository.Any(c => c.Id == id);
                if (!exists) return new Response { Message = $"CategoryEntity '{id}' not exist." };

                var pcs = _postCategoryRepository.Get(pc => pc.CategoryId == id);
                if (null != pcs)
                {
                    await _postCategoryRepository.DeleteAsync(pcs);
                }

                _categoryRepository.Delete(id);
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CategoryDeleted, $"Category '{id}' deleted.");

                return new SuccessResponse();
            });
        }

        public Task<Response> UpdateCategoryAsync(EditCategoryRequest editCategoryRequest)
        {
            return TryExecuteAsync(async () =>
            {
                var cat = _categoryRepository.Get(editCategoryRequest.Id);
                if (null == cat) return new Response { Message = $"CategoryEntity id '{editCategoryRequest.Id}' not found." };

                cat.Title = editCategoryRequest.Title.Trim();
                cat.DisplayName = editCategoryRequest.DisplayName.Trim();
                cat.Note = editCategoryRequest.Note.Trim();

                await _categoryRepository.UpdateAsync(cat);
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CategoryUpdated, $"Category '{editCategoryRequest.Id}' updated.");

                return new SuccessResponse();
            });
        }
    }
}