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

        public Task<Response<IReadOnlyList<Category>>> GetAllAsync()
        {
            return TryExecuteAsync<IReadOnlyList<Category>>(async () =>
            {
                var list = await _categoryRepository.SelectAsync(c => new Category
                {
                    Id = c.Id,
                    DisplayName = c.DisplayName,
                    RouteName = c.RouteName,
                    Note = c.Note
                });
                return new SuccessResponse<IReadOnlyList<Category>>(list);
            });
        }

        public Task<Response<Category>> GetAsync(string categoryName)
        {
            return TryExecuteAsync<Category>(async () =>
            {
                var cat = await _categoryRepository.SelectFirstOrDefaultAsync(
                    new CategorySpec(categoryName), category =>
                        new Category
                        {
                            DisplayName = category.DisplayName,
                            Id = category.Id,
                            RouteName = category.RouteName,
                            Note = category.Note
                        });

                return new SuccessResponse<Category>(cat);
            }, keyParameter: categoryName);
        }

        public Task<Response<Category>> GetAsync(Guid categoryId)
        {
            return TryExecuteAsync<Category>(async () =>
            {
                var cat = await _categoryRepository.SelectFirstOrDefaultAsync(
                    new CategorySpec(categoryId), category =>
                        new Category
                        {
                            DisplayName = category.DisplayName,
                            Id = category.Id,
                            RouteName = category.RouteName,
                            Note = category.Note
                        });

                return new SuccessResponse<Category>(cat);
            }, keyParameter: categoryId);
        }

        public Task<Response> CreateAsync(CreateCategoryRequest createCategoryRequest)
        {
            return TryExecuteAsync(async () =>
            {
                var exists = _categoryRepository.Any(c => c.RouteName == createCategoryRequest.RouteName);
                if (exists)
                {
                    return new Response { Message = $"CategoryEntity titled '{createCategoryRequest.RouteName}' already exist." };
                }

                var category = new CategoryEntity
                {
                    Id = Guid.NewGuid(),
                    RouteName = createCategoryRequest.RouteName.Trim(),
                    Note = createCategoryRequest.Note.Trim(),
                    DisplayName = createCategoryRequest.DisplayName.Trim()
                };

                await _categoryRepository.AddAsync(category);
                await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.CategoryCreated, $"Category '{category.RouteName}' created");

                return new SuccessResponse();
            });
        }

        public Task<Response> DeleteAsync(Guid id)
        {
            return TryExecuteAsync(async () =>
            {
                var exists = _categoryRepository.Any(c => c.Id == id);
                if (!exists) return new Response { Message = $"CategoryEntity '{id}' not exist." };

                var pcs = await _postCategoryRepository.GetAsync(pc => pc.CategoryId == id);
                if (null != pcs)
                {
                    await _postCategoryRepository.DeleteAsync(pcs);
                }

                _categoryRepository.Delete(id);
                await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.CategoryDeleted, $"Category '{id}' deleted.");

                return new SuccessResponse();
            });
        }

        public Task<Response> UpdateAsync(EditCategoryRequest editCategoryRequest)
        {
            return TryExecuteAsync(async () =>
            {
                var cat = await _categoryRepository.GetAsync(editCategoryRequest.Id);
                if (null == cat) return new Response { Message = $"CategoryEntity id '{editCategoryRequest.Id}' not found." };

                cat.RouteName = editCategoryRequest.RouteName.Trim();
                cat.DisplayName = editCategoryRequest.DisplayName.Trim();
                cat.Note = editCategoryRequest.Note.Trim();

                await _categoryRepository.UpdateAsync(cat);
                await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.CategoryUpdated, $"Category '{editCategoryRequest.Id}' updated.");

                return new SuccessResponse();
            });
        }
    }
}