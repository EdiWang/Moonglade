using System;
using System.Collections.Generic;
using System.Text.Json;
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

        public Task<Response<IReadOnlyList<Category>>> GetCategoryListAsync()
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

                Logger.LogInformation($"Adding new categoryEntity to database: {JsonSerializer.Serialize(category)}");
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
                if (null != pcs)
                {
                    _postCategoryRepository.Delete(pcs);
                }

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