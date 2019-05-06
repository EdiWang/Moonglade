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
    public class TagService : MoongladeService
    {
        private readonly IRepository<Tag> _tagRepository;
        private readonly IRepository<PostTag> _postTagRepository;

        public TagService(
            ILogger<TagService> logger,
            IRepository<Tag> tagRepository,
            IRepository<PostTag> postTagRepository) : base(logger)
        {
            _tagRepository = tagRepository;
            _postTagRepository = postTagRepository;
        }

        public Task<IReadOnlyList<Tag>> GetAllTagsAsync()
        {
            return _tagRepository.GetAsync();
        }

        public Task<IReadOnlyList<string>> GetAllTagNamesAsync()
        {
            return _tagRepository.SelectAsync(t => t.DisplayName);
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
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
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
                int rows = _tagRepository.Delete(tagId);
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Deleting Tag.");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response<IReadOnlyList<TagInfo>>> GetHotTagsAsync(int top)
        {
            try
            {
                if (_tagRepository.Any())
                {
                    var spec = new TagSpec(top);
                    var hotTags = await _tagRepository.SelectAsync(spec, t => new TagInfo
                    {
                        TagCount = t.PostTag.Count,
                        TagName = t.DisplayName,
                        NormalizedTagName = t.NormalizedName
                    });

                    return new SuccessResponse<IReadOnlyList<TagInfo>>(hotTags);
                }

                return new SuccessResponse<IReadOnlyList<TagInfo>>(new List<TagInfo>());
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetHotTagsAsync)}");
                return new FailedResponse<IReadOnlyList<TagInfo>>((int)ResponseFailureCode.GeneralException, e.Message);
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
                return new FailedResponse<IReadOnlyList<TagInfo>>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }
    }
}
