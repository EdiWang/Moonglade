using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class TagService : MoongladeService
    {
        public TagService(MoongladeDbContext context, ILogger<TagService> logger) : base(context, logger)
        {
        }

        public IQueryable<Tag> GetTags()
        {
            return Context.Tag;
        }

        public Tag GetTag(int tagId)
        {
            return Context.Tag.Find(tagId);
        }

        public Response UpdateTag(int tagId, string newName)
        {
            try
            {
                Logger.LogInformation($"Updating tag {tagId} with new name {newName}");
                var tag = GetTag(tagId);
                if (null != tag)
                {
                    tag.DisplayName = newName;
                    tag.NormalizedName = Utils.NormalizeTagName(newName);
                    var rows = Context.SaveChanges();
                    return new Response(rows > 0);
                }
                return new FailedResponse((int)ResponseFailureCode.TagNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Updating Tag.");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response Delete(int tagId)
        {
            try
            {
                // 1. Delete Post-Tag Association
                var postTags = Context.PostTag.Where(pt => pt.TagId == tagId);
                Context.RemoveRange(postTags);
                Context.SaveChanges();

                // 2. Delte Tag itslef
                var tag = Context.Tag.Find(tagId);
                if (null != tag)
                {
                    Context.Remove(tag);
                    int rows = Context.SaveChanges();
                    return new Response(rows > 0);
                }
                return new FailedResponse((int)ResponseFailureCode.TagNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Deleting Tag.");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public async Task<Response<List<TagInfo>>> GetHotTagsAsync(int top)
        {
            try
            {
                if (Context.Tag.Any())
                {
                    var hotTags = Context.Tag.OrderByDescending(p => p.PostTag.Count)
                                             .Take(top).AsNoTracking()
                                             .Select(t => new TagInfo
                                             {
                                                 TagCount = t.PostTag.Count,
                                                 TagName = t.DisplayName,
                                                 NormalizedTagName = t.NormalizedName
                                             });

                    var list = await hotTags.ToListAsync();
                    return new SuccessResponse<List<TagInfo>>(list);
                }

                return new SuccessResponse<List<TagInfo>>(new List<TagInfo>());
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetHotTagsAsync)}");
                return new FailedResponse<List<TagInfo>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Tag GetTag(string normalizedName)
        {
            var tag = Context.Tag.FirstOrDefault(t => t.NormalizedName == normalizedName);
            return tag;
        }

        public async Task<Response<List<TagInfo>>> GetTagCountListAsync()
        {
            try
            {
                var queryTag = Context.Tag.AsNoTracking().Select(t => new TagInfo
                {
                    TagName = t.DisplayName,
                    NormalizedTagName = t.NormalizedName,
                    TagCount = t.PostTag.Count
                });

                var list = await queryTag.ToListAsync();
                return new SuccessResponse<List<TagInfo>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetTagCountListAsync)}");
                return new FailedResponse<List<TagInfo>>((int)ResponseFailureCode.GeneralException);
            }
        }
    }
}
