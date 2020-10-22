using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class TagService : BlogService
    {
        private readonly IRepository<TagEntity> _tagRepository;
        private readonly IRepository<PostTagEntity> _postTagRepository;
        private readonly IBlogAudit _blogAudit;

        public TagService(
            ILogger<TagService> logger,
            IRepository<TagEntity> tagRepository,
            IRepository<PostTagEntity> postTagRepository,
            IBlogAudit blogAudit) : base(logger)
        {
            _tagRepository = tagRepository;
            _postTagRepository = postTagRepository;
            _blogAudit = blogAudit;
        }

        public Task<IReadOnlyList<Tag>> GetAllAsync()
        {
            return _tagRepository.SelectAsync(t => new Tag
            {
                Id = t.Id,
                NormalizedName = t.NormalizedName,
                DisplayName = t.DisplayName
            });
        }

        public Task<IReadOnlyList<string>> GetAllNamesAsync()
        {
            return _tagRepository.SelectAsync(t => t.DisplayName);
        }

        public async Task UpdateAsync(int tagId, string newName)
        {
            Logger.LogInformation($"Updating tag {tagId} with new name {newName}");
            var tag = await _tagRepository.GetAsync(tagId);
            if (null == tag) return;

            tag.DisplayName = newName;
            tag.NormalizedName = NormalizeTagName(newName);
            await _tagRepository.UpdateAsync(tag);
            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.TagUpdated, $"Tag id '{tagId}' is updated.");
        }

        public async Task DeleteAsync(int tagId)
        {
            // 1. Delete Post-Tag Association
            var postTags = await _postTagRepository.GetAsync(new PostTagSpec(tagId));
            await _postTagRepository.DeleteAsync(postTags);

            // 2. Delte Tag itslef
            await _tagRepository.DeleteAsync(tagId);
            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.TagDeleted, $"Tag id '{tagId}' is deleted");
        }

        public async Task<IReadOnlyList<DegreeTag>> GetHotTagsAsync(int top)
        {
            if (!_tagRepository.Any()) return new List<DegreeTag>();

            var spec = new TagSpec(top);
            var tags = await _tagRepository.SelectAsync(spec, t => new DegreeTag
            {
                Degree = t.PostTag.Count,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            });

            return tags;
        }

        public Tag Get(string normalizedName)
        {
            var tag = _tagRepository.SelectFirstOrDefault(new TagSpec(normalizedName), tg => new Tag
            {
                Id = tg.Id,
                NormalizedName = tg.NormalizedName,
                DisplayName = tg.DisplayName
            });
            return tag;
        }

        public Task<IReadOnlyList<DegreeTag>> GetTagCountListAsync()
        {
            return _tagRepository.SelectAsync(t => new DegreeTag
            {
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName,
                Degree = t.PostTag.Count
            });
        }

        private static readonly Tuple<string, string>[] TagNormalizeSourceTable =
        {
            Tuple.Create(".", "dot"),
            Tuple.Create("#", "sharp"),
            Tuple.Create(" ", "-")
        };

        public static string NormalizeTagName(string orgTagName)
        {
            var result = new StringBuilder(orgTagName);
            foreach (var (item1, item2) in TagNormalizeSourceTable)
            {
                result.Replace(item1, item2);
            }
            return result.ToString().ToLower();
        }

        public static bool ValidateTagName(string tagDisplayName)
        {
            if (string.IsNullOrWhiteSpace(tagDisplayName))
            {
                return false;
            }

            // Regex performance best practice
            // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices

            const string pattern = @"^[a-zA-Z 0-9\.\-\+\#\s]*$";
            return Regex.IsMatch(tagDisplayName, pattern);
        }
    }
}
