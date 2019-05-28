using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
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

        public CategoryService(ILogger<CategoryService> logger,
            IRepository<CategoryEntity> categoryRepository,
            IRepository<PostCategoryEntity> postCategoryRepository) : base(logger)
        {
            _categoryRepository = categoryRepository;
            _postCategoryRepository = postCategoryRepository;
        }

        public async Task<Response<IReadOnlyList<CategoryEntity>>> GetAllCategoriesAsync()
        {
            return await TryExecuteAsync<IReadOnlyList<CategoryEntity>>(async () =>
            {
                var item = await _categoryRepository.GetAsync();
                return new SuccessResponse<IReadOnlyList<CategoryEntity>>(item);
            });
        }

        public async Task<Response<CategoryInfo>> GetCategoryAsync(string categoryName)
        {
            return await TryExecuteAsync<CategoryInfo>(async () =>
            {
                var cat = await _categoryRepository.SelectFirstOrDefaultAsync(
                    new CategorySpec(categoryName), category =>
                        new CategoryInfo
                        {
                            DisplayName = category.DisplayName,
                            Id = category.Id,
                            Name = category.Title,
                            Note = category.Note
                        });

                return new SuccessResponse<CategoryInfo>(cat);
            }, keyParameter: categoryName);
        }

        public async Task<Response<CategoryInfo>> GetCategoryAsync(Guid categoryId)
        {
            return await TryExecuteAsync<CategoryInfo>(async () =>
            {
                var cat = await _categoryRepository.SelectFirstOrDefaultAsync(
                    new CategorySpec(categoryId), category =>
                        new CategoryInfo
                        {
                            DisplayName = category.DisplayName,
                            Id = category.Id,
                            Name = category.Title,
                            Note = category.Note
                        });

                return new SuccessResponse<CategoryInfo>(cat);
            }, keyParameter: categoryId);
        }

        public async Task<Response<IReadOnlyList<CategoryInfo>>> GetCategoryListAsync()
        {
            return await TryExecuteAsync<IReadOnlyList<CategoryInfo>>(async () =>
            {
                var list = await _categoryRepository.SelectAsync(c => new CategoryInfo
                {
                    Id = c.Id,
                    DisplayName = c.DisplayName,
                    Name = c.Title,
                    Note = c.Note
                });

                return new SuccessResponse<IReadOnlyList<CategoryInfo>>(list);
            });
        }

        public Response CreateCategory(CreateCategoryRequest createCategoryRequest)
        {
            return TryExecute(() =>
            {
                var exists = _categoryRepository.Any(c => c.Title == createCategoryRequest.Title);
                if (exists)
                {
                    return new Response { Message = $"CategoryEntity titled {createCategoryRequest.Title} already exists." };
                }

                var category = new CategoryEntity
                {
                    Id = Guid.NewGuid(),
                    Title = createCategoryRequest.Title,
                    Note = createCategoryRequest.Note,
                    DisplayName = createCategoryRequest.DisplayName
                };

                Logger.LogInformation("Adding new categoryEntity to database.");
                _categoryRepository.Add(category);
                return new SuccessResponse();
            });
        }

        public Response Delete(Guid id)
        {
            return TryExecute(() =>
            {
                var exists = _categoryRepository.Any(c => c.Id == id);
                if (!exists) return new Response { Message = $"CategoryEntity ID {id} not exists." };

                Logger.LogInformation($"Removing Post-Category associations for category id: {id}");
                var pcs = _postCategoryRepository.Get(pc => pc.CategoryId == id);
                _postCategoryRepository.Delete(pcs);

                Logger.LogInformation($"Removing categoryEntity {id}");
                _categoryRepository.Delete(id);
                return new SuccessResponse();
            });
        }

        public Response UpdateCategory(EditCategoryRequest editCategoryRequest)
        {
            return TryExecute(() =>
            {
                var cat = _categoryRepository.Get(editCategoryRequest.Id);
                if (null == cat) return new Response { Message = $"CategoryEntity id {editCategoryRequest.Id} not found." };

                cat.Title = editCategoryRequest.Title;
                cat.DisplayName = editCategoryRequest.DisplayName;
                cat.Note = editCategoryRequest.Note;

                var rows = _categoryRepository.Update(cat);
                return new Response(rows > 0);
            });
        }
    }
}