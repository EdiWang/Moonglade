using System;
using System.Collections.Generic;
using System.Linq;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class CategoryService : MoongladeService
    {
        public CategoryService(MoongladeDbContext context, ILogger<CategoryService> logger) : base(context, logger)
        {
        }

        public Response<List<Category>> GetAllCategories()
        {
            try
            {
                var item = Context.Category.ToList();
                return new SuccessResponse<List<Category>>(item);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetAllCategories)}");
                return new FailedResponse<List<Category>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public int GetPostCountByCategoryId(Guid catId)
        {
            var count = Context.PostCategory.Count(c => c.CategoryId == catId);
            return count;
        }

        public IQueryable<Category> GetCategoriesAsQueryable()
        {
            return Context.Category;
        }

        public Response<Category> GetCategory(Guid categoryId)
        {
            try
            {
                var cat = Context.Category.Find(categoryId);
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

        public Response<List<CategoryInfo>> GetCategoryList()
        {
            try
            {
                var query = Context.Category.Select(c => new CategoryInfo
                {
                    Id = c.Id,
                    DisplayName = c.DisplayName,
                    Name = c.Title,
                    Note = c.Note
                });

                var list = query.ToList();
                return new SuccessResponse<List<CategoryInfo>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetCategoryList)}");
                return new FailedResponse<List<CategoryInfo>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<List<ArchiveItem>> GetArchiveList()
        {
            try
            {
                if (!Context.Post.Any())
                {
                    return new SuccessResponse<List<ArchiveItem>>();
                }

                var query = from post in Context.Post
                            group post by new
                            {
                                year = post.PostPublish.PubDateUtc.Value.Year,
                                month = post.PostPublish.PubDateUtc.Value.Month
                            }
                            into monthList
                            select new ArchiveItem
                            {
                                Year = monthList.Key.year,
                                Month = monthList.Key.month,
                                Count = monthList.Select(p => p.Id).Count()
                            };

                var list = query.ToList();
                return new SuccessResponse<List<ArchiveItem>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetArchiveList)}");
                return new FailedResponse<List<ArchiveItem>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response CreateCategory(Category category)
        {
            try
            {
                var cat = Context.Category.FirstOrDefault(c => c.Title == category.Title);
                if (null != cat)
                {
                    return new Response { Message = $"Category titled {category.Title} already exists." };
                }

                Logger.LogInformation("Adding new category to database.");
                Context.Category.Add(category);
                var rows = Context.SaveChanges();
                return new Response(rows > 0);
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
                var cat = Context.Category.Find(id);
                if (null == cat)
                {
                    return new Response { Message = $"Category ID {id} not exists." };
                }

                Logger.LogInformation($"Removing Post-Category associations for category id: {id}");
                var pcs = Context.PostCategory.Where(pc => pc.CategoryId == id);
                Context.PostCategory.RemoveRange(pcs);
                Context.SaveChanges();

                Logger.LogInformation($"Removing category {id}");
                Context.Category.Remove(cat);
                int rows = Context.SaveChanges();
                return new Response(rows > 0);
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
                var cat = Context.Category.Find(category.Id);
                if (null == cat)
                {
                    return new Response { Message = $"Category id {category.Id} not found." };
                }

                cat.Title = category.Title;
                cat.DisplayName = category.DisplayName;
                cat.Note = category.Note;

                int rows = Context.SaveChanges();
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
