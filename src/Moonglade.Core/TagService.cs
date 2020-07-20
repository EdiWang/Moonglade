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
    public class TagService : MoongladeService
    {
        private readonly IRepository<TagEntity> _tagRepository;
        private readonly IRepository<PostTagEntity> _postTagRepository;
        private readonly IMoongladeAudit _moongladeAudit;

        public TagService(
            ILogger<TagService> logger,
            IRepository<TagEntity> tagRepository,
            IRepository<PostTagEntity> postTagRepository,
            IMoongladeAudit moongladeAudit) : base(logger)
        {
            _tagRepository = tagRepository;
            _postTagRepository = postTagRepository;
            _moongladeAudit = moongladeAudit;
        }

        public Task<Response<IReadOnlyList<Tag>>> GetAllAsync()
        {
            return TryExecuteAsync<IReadOnlyList<Tag>>(async () =>
            {
                var list = await _tagRepository.SelectAsync(t => new Tag
                {
                    Id = t.Id,
                    NormalizedName = t.NormalizedName,
                    DisplayName = t.DisplayName
                });

                return new SuccessResponse<IReadOnlyList<Tag>>(list);
            });
        }

        public Task<Response<IReadOnlyList<string>>> GetAllNamesAsync()
        {
            return TryExecuteAsync<IReadOnlyList<string>>(async () =>
            {
                var tagNames = await _tagRepository.SelectAsync(t => t.DisplayName);
                return new SuccessResponse<IReadOnlyList<string>>(tagNames);
            });
        }

        public Task<Response> UpdateAsync(int tagId, string newName)
        {
            return TryExecuteAsync(async () =>
            {
                Logger.LogInformation($"Updating tag {tagId} with new name {newName}");
                var tag = await _tagRepository.GetAsync(tagId);
                if (null != tag)
                {
                    tag.DisplayName = newName;
                    tag.NormalizedName = Utils.NormalizeTagName(newName);
                    await _tagRepository.UpdateAsync(tag);
                    await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.TagUpdated, $"Tag id '{tagId}' is updated.");

                    return new SuccessResponse();
                }

                return new FailedResponse((int)ResponseFailureCode.TagNotFound);
            });
        }

        public Task<Response> DeleteAsync(int tagId)
        {
            return TryExecuteAsync(async () =>
            {
                // 1. Delete Post-Tag Association
                var postTags = await _postTagRepository.GetAsync(new PostTagSpec(tagId));
                await _postTagRepository.DeleteAsync(postTags);

                // 2. Delte Tag itslef
                await _tagRepository.DeleteAsync(tagId);
                await _moongladeAudit.AddAuditEntry(EventType.Content, AuditEventId.TagDeleted, $"Tag id '{tagId}' is deleted");

                return new SuccessResponse();
            });
        }

        public Task<Response<IReadOnlyList<DegreeTag>>> GetHotTagsAsync(int top)
        {
            return TryExecuteAsync<IReadOnlyList<DegreeTag>>(async () =>
            {
                if (_tagRepository.Any())
                {
                    var spec = new TagSpec(top);
                    var hotTags = await _tagRepository.SelectAsync(spec, t => new DegreeTag
                    {
                        Degree = t.PostTag.Count,
                        DisplayName = t.DisplayName,
                        NormalizedName = t.NormalizedName
                    });

                    return new SuccessResponse<IReadOnlyList<DegreeTag>>(hotTags);
                }

                return new SuccessResponse<IReadOnlyList<DegreeTag>>(new List<DegreeTag>());
            }, keyParameter: top);
        }

        public Response<Tag> Get(string normalizedName)
        {
            return TryExecute(() =>
            {
                var tag = _tagRepository.SelectFirstOrDefault(new TagSpec(normalizedName), tg => new Tag
                {
                    Id = tg.Id,
                    NormalizedName = tg.NormalizedName,
                    DisplayName = tg.DisplayName
                });
                return new SuccessResponse<Tag>(tag);
            });
        }

        public Task<Response<IReadOnlyList<DegreeTag>>> GetTagCountListAsync()
        {
            return TryExecuteAsync<IReadOnlyList<DegreeTag>>(async () =>
            {
                var list = await _tagRepository.SelectAsync(t => new DegreeTag
                {
                    DisplayName = t.DisplayName,
                    NormalizedName = t.NormalizedName,
                    Degree = t.PostTag.Count
                });

                return new SuccessResponse<IReadOnlyList<DegreeTag>>(list);
            });
        }
    }
}
