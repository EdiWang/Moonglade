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
        private readonly IRepository<TagEntity> _tagRepository;
        private readonly IRepository<PostTagEntity> _postTagRepository;

        public TagService(
            ILogger<TagService> logger,
            IRepository<TagEntity> tagRepository,
            IRepository<PostTagEntity> postTagRepository) : base(logger)
        {
            _tagRepository = tagRepository;
            _postTagRepository = postTagRepository;
        }

        public Task<IReadOnlyList<TagEntity>> GetAllTagsAsync()
        {
            return _tagRepository.GetAsync();
        }

        public Task<IReadOnlyList<string>> GetAllTagNamesAsync()
        {
            return _tagRepository.SelectAsync(t => t.DisplayName);
        }

        public Response UpdateTag(int tagId, string newName)
        {
            return TryExecute(() =>
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
            });
        }

        public Response Delete(int tagId)
        {
            return TryExecute(() =>
            {
                // 1. Delete Post-Tag Association
                var postTags = _postTagRepository.Get(new PostTagSpec(tagId));
                _postTagRepository.Delete(postTags);

                // 2. Delte Tag itslef
                int rows = _tagRepository.Delete(tagId);
                return new Response(rows > 0);
            });
        }

        public async Task<Response<IReadOnlyList<TagInfo>>> GetHotTagsAsync(int top)
        {
            return await TryExecuteAsync<IReadOnlyList<TagInfo>>(async () =>
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
            }, keyParameter: top);
        }

        public TagInfo GetTag(string normalizedName)
        {
            var tag = _tagRepository.SelectFirstOrDefault(new TagSpec(normalizedName), tg => new TagInfo()
            {
                Id = tg.Id,
                NormalizedTagName = tg.NormalizedName,
                TagName = tg.DisplayName
            });
            return tag;
        }

        public async Task<Response<IReadOnlyList<TagInfo>>> GetTagCountListAsync()
        {
            return await TryExecuteAsync<IReadOnlyList<TagInfo>>(async () =>
            {
                var list = await _tagRepository.SelectAsync(t => new TagInfo
                {
                    TagName = t.DisplayName,
                    NormalizedTagName = t.NormalizedName,
                    TagCount = t.PostTag.Count
                });

                return new SuccessResponse<IReadOnlyList<TagInfo>>(list);
            });
        }
    }
}
