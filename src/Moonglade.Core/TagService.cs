using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class TagService : MoongladeService
    {
        private readonly IRepository<Tag> _tagRepository;
        private readonly IRepository<PostTag> _postTagRepository;

        public TagService(MoongladeDbContext context, ILogger<TagService> logger, 
            IRepository<Tag> tagRepository, IRepository<PostTag> postTagRepository) : base(context, logger)
        {
            _tagRepository = tagRepository;
            _postTagRepository = postTagRepository;
        }

        public IQueryable<Tag> GetTags()
        {
            return Context.Tag;
        }

        public IReadOnlyList<string> GetAllTagNames()
        {
            return Context.Tag.Select(t => t.DisplayName).ToList();
        }

        public Response UpdateTag(int tagId, string newName)
        {
            try
            {
                Logger.LogInformation($"Updating tag {tagId} with new name {newName}");
                var tag = _tagRepository.Get(tagId);
                if (null != tag)
                {
                    tag.DisplayName = newName;
                    tag.NormalizedName = Utils.NormalizeTagName(newName);
                    var rows = _tagRepository.Update(tag);
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
                var postTags = _postTagRepository.Get(new PostTagSpec(tagId));
                _postTagRepository.Delete(postTags);

                // 2. Delte Tag itslef
                var tag = Context.Tag.Find(tagId);
                int rows = _tagRepository.Delete(tag);
                return new Response(rows > 0);
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
                    // TODO: Refact this Context to Repository
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
            var tag = _tagRepository.Get(t => t.NormalizedName == normalizedName);
            return tag;
        }

        public async Task<Response<IReadOnlyList<TagInfo>>> GetTagCountListAsync()
        {
            try
            {
                var list = await _tagRepository.SelectAsync(t => new TagInfo
                {
                    TagName = t.DisplayName,
                    NormalizedTagName = t.NormalizedName,
                    TagCount = t.PostTag.Count
                });

                return new SuccessResponse<IReadOnlyList<TagInfo>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetTagCountListAsync)}");
                return new FailedResponse<IReadOnlyList<TagInfo>>((int)ResponseFailureCode.GeneralException);
            }
        }
    }
}
