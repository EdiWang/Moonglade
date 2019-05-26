using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IRepository<PostEntity> _postRepository;

        private readonly IRepository<CategoryEntity> _categoryRepository;

        private readonly IRepository<PostCategoryEntity> _postCategoryRepository;

        public CategoryService(ILogger<CategoryService> logger,
            IRepository<CategoryEntity> categoryRepository,
            IRepository<PostCategoryEntity> postCategoryRepository,
            IRepository<PostEntity> postRepository) : base(logger)
        {
            _categoryRepository = categoryRepository;
            _postCategoryRepository = postCategoryRepository;
            _postRepository = postRepository;
        }

        public async Task<Response<IReadOnlyList<CategoryEntity>>> GetAllCategoriesAsync()
        {
            try
            {
                var item = await _categoryRepository.GetAsync();
                return new SuccessResponse<IReadOnlyList<CategoryEntity>>(item);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetAllCategoriesAsync)}");
                return new FailedResponse<IReadOnlyList<CategoryEntity>>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response<CategoryInfo>> GetCategoryAsync(string categoryName)
        {
            try
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
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetCategoryAsync)}");
                return new FailedResponse<CategoryInfo>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response<CategoryInfo>> GetCategoryAsync(Guid categoryId)
        {
            try
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
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetCategoryAsync)}");
                return new FailedResponse<CategoryInfo>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response<IReadOnlyList<CategoryInfo>>> GetCategoryListAsync()
        {
            try
            {
                var list = await _categoryRepository.SelectAsync(c => new CategoryInfo
                {
                    Id = c.Id,
                    DisplayName = c.DisplayName,
                    Name = c.Title,
                    Note = c.Note
                });

                return new SuccessResponse<IReadOnlyList<CategoryInfo>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetCategoryListAsync)}");
                return new FailedResponse<IReadOnlyList<CategoryInfo>>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response<IReadOnlyList<ArchiveItem>>> GetArchiveListAsync()
        {
            try
            {
                if (!_postRepository.Any(p =>
                    p.PostPublish.IsPublished && !p.PostPublish.IsDeleted))
                {
                    return new SuccessResponse<IReadOnlyList<ArchiveItem>>();
                }

                var list = await _postRepository.SelectAsync(post => new
                {
                    year = post.PostPublish.PubDateUtc.Value.Year,
                    month = post.PostPublish.PubDateUtc.Value.Month
                }, monthList => new ArchiveItem
                {
                    Year = monthList.Key.year,
                    Month = monthList.Key.month,
                    Count = monthList.Select(p => p.Id).Count()
                });

                return new SuccessResponse<IReadOnlyList<ArchiveItem>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetArchiveListAsync)}");
                return new FailedResponse<IReadOnlyList<ArchiveItem>>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public Response CreateCategory(CategoryEntity categoryEntity)
        {
            return TryExecute(() =>
            {
                var exists = _categoryRepository.Any(c => c.Title == categoryEntity.Title);
                if (exists)
                {
                    return new Response { Message = $"CategoryEntity titled {categoryEntity.Title} already exists." };
                }

                Logger.LogInformation("Adding new categoryEntity to database.");
                _categoryRepository.Add(categoryEntity);
                return new SuccessResponse();
            });
        }

        public Response Delete(Guid id)
        {
            return TryExecute(() =>
            {
                var exists = _categoryRepository.Any(c => c.Id == id);
                if (!exists)
                {
                    return new Response { Message = $"CategoryEntity ID {id} not exists." };
                }

                Logger.LogInformation($"Removing Post-Category associations for category id: {id}");
                var pcs = _postCategoryRepository.Get(pc => pc.CategoryId == id);
                _postCategoryRepository.Delete(pcs);

                Logger.LogInformation($"Removing categoryEntity {id}");
                _categoryRepository.Delete(id);
                return new SuccessResponse();
            });
        }

        public Response UpdateCategory(CategoryEntity categoryEntity)
        {
            return TryExecute(() =>
            {
                var cat = _categoryRepository.Get(categoryEntity.Id);
                if (null == cat)
                {
                    return new Response { Message = $"CategoryEntity id {categoryEntity.Id} not found." };
                }

                cat.Title = categoryEntity.Title;
                cat.DisplayName = categoryEntity.DisplayName;
                cat.Note = categoryEntity.Note;

                int rows = _categoryRepository.Update(cat);
                return new Response(rows > 0);
            });
        }
    }
}
