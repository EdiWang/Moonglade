using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class CategoryService : MoongladeService
    {
        private readonly IRepository<Post> _postRepository;

        private readonly IRepository<Category> _categoryRepository;

        private readonly IRepository<PostCategory> _postCategoryRepository;

        public CategoryService(ILogger<CategoryService> logger,
            IRepository<Category> categoryRepository,
            IRepository<PostCategory> postCategoryRepository,
            IRepository<Post> postRepository) : base(logger: logger)
        {
            _categoryRepository = categoryRepository;
            _postCategoryRepository = postCategoryRepository;
            _postRepository = postRepository;
        }

        public async Task<Response<IReadOnlyList<Category>>> GetAllCategoriesAsync()
        {
            try
            {
                var item = await _categoryRepository.GetAsync();
                return new SuccessResponse<IReadOnlyList<Category>>(item);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetAllCategoriesAsync)}");
                return new FailedResponse<IReadOnlyList<Category>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public int GetPostCountByCategoryId(Guid catId)
        {
            return _postCategoryRepository.Count(c => c.CategoryId == catId);
        }

        public Response<Category> GetCategory(string categoryName)
        {
            try
            {
                var cat = _categoryRepository.Get(p =>
                    string.Compare(p.Title, categoryName, StringComparison.OrdinalIgnoreCase) == 0);

                return new SuccessResponse<Category>(cat);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetCategory)}");
                return new FailedResponse<Category>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<Category> GetCategory(Guid categoryId)
        {
            try
            {
                var cat = _categoryRepository.Get(categoryId);
                if (null != cat)
                {
                    return new SuccessResponse<Category>(cat);
                }

                return new FailedResponse<Category>((int)ResponseFailureCode.CategoryNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetCategory)}");
                return new FailedResponse<Category>((int)ResponseFailureCode.GeneralException);
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
                return new FailedResponse<IReadOnlyList<CategoryInfo>>((int)ResponseFailureCode.GeneralException);
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
                return new FailedResponse<IReadOnlyList<ArchiveItem>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response CreateCategory(Category category)
        {
            try
            {
                var exists = _categoryRepository.Any(c => c.Title == category.Title);
                if (exists)
                {
                    return new Response { Message = $"Category titled {category.Title} already exists." };
                }

                Logger.LogInformation("Adding new category to database.");
                _categoryRepository.Add(category);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(CreateCategory)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response Delete(Guid id)
        {
            try
            {
                var exists = _categoryRepository.Any(c => c.Id == id);
                if (!exists)
                {
                    return new Response { Message = $"Category ID {id} not exists." };
                }

                Logger.LogInformation($"Removing Post-Category associations for category id: {id}");
                var pcs = _postCategoryRepository.Get(pc => pc.CategoryId == id);
                _postCategoryRepository.Delete(pcs);

                Logger.LogInformation($"Removing category {id}");
                _categoryRepository.Delete(id);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(Delete)}(id: {id})");
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        public Response UpdateCategory(Category category)
        {
            try
            {
                var cat = _categoryRepository.Get(category.Id);
                if (null == cat)
                {
                    return new Response { Message = $"Category id {category.Id} not found." };
                }

                cat.Title = category.Title;
                cat.DisplayName = category.DisplayName;
                cat.Note = category.Note;

                int rows = _categoryRepository.Update(cat);
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(UpdateCategory)}.");
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }
    }
}
