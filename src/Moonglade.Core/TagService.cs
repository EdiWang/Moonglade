using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class TagService : BlogService
    {
        private readonly AppSettings _settings;
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IBlogAudit _audit;
        private readonly IOptions<List<TagNormalization>> _tagNormalization;

        public TagService(
            IOptions<AppSettings> settings,
            IRepository<TagEntity> tagRepo,
            IRepository<PostTagEntity> postTagRepo,
            IBlogAudit audit,
            IOptions<List<TagNormalization>> tagNormalization)
        {
            _settings = settings.Value;
            _tagRepo = tagRepo;
            _postTagRepo = postTagRepo;
            _audit = audit;
            _tagNormalization = tagNormalization;
        }

        public Task<IReadOnlyList<Tag>> GetAllAsync()
        {
            return _tagRepo.SelectAsync(t => new Tag
            {
                Id = t.Id,
                NormalizedName = t.NormalizedName,
                DisplayName = t.DisplayName
            });
        }

        public Task<IReadOnlyList<string>> GetAllNamesAsync()
        {
            return _tagRepo.SelectAsync(t => t.DisplayName);
        }

        public async Task UpdateAsync(int tagId, string newName)
        {
            var tag = await _tagRepo.GetAsync(tagId);
            if (null == tag) return;

            tag.DisplayName = newName;
            tag.NormalizedName = NormalizeTagName(newName, _tagNormalization.Value);
            await _tagRepo.UpdateAsync(tag);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.TagUpdated, $"Tag id '{tagId}' is updated.");
        }

        public async Task DeleteAsync(int tagId)
        {
            // 1. Delete Post-Tag Association
            var postTags = await _postTagRepo.GetAsync(new PostTagSpec(tagId));
            await _postTagRepo.DeleteAsync(postTags);

            // 2. Delte Tag itslef
            await _tagRepo.DeleteAsync(tagId);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.TagDeleted, $"Tag id '{tagId}' is deleted");
        }

        public async Task<IReadOnlyList<DegreeTag>> GetHotTagsAsync(int top)
        {
            if (!_tagRepo.Any()) return new List<DegreeTag>();

            var spec = new TagSpec(top);
            var tags = await _tagRepo.SelectAsync(spec, t => new DegreeTag
            {
                Degree = t.PostTag.Count,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            });

            return tags;
        }

        public Tag Get(string normalizedName)
        {
            var tag = _tagRepo.SelectFirstOrDefault(new TagSpec(normalizedName), tg => new Tag
            {
                Id = tg.Id,
                NormalizedName = tg.NormalizedName,
                DisplayName = tg.DisplayName
            });
            return tag;
        }

        public Task<IReadOnlyList<DegreeTag>> GetTagCountListAsync()
        {
            return _tagRepo.SelectAsync(t => new DegreeTag
            {
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName,
                Degree = t.PostTag.Count
            });
        }

        public static string NormalizeTagName(string orgTagName, IList<TagNormalization> normalizations)
        {
            var isEnglishName = Regex.IsMatch(orgTagName, @"^[a-zA-Z 0-9\.\-\+\#\s]*$");
            if (isEnglishName)
            {
                var result = new StringBuilder(orgTagName);
                foreach (var item in normalizations)
                {
                    result.Replace(item.Source, item.Target);
                }
                return result.ToString().ToLower();
            }

            var bytes = Encoding.Unicode.GetBytes(orgTagName);
            var hexArray = bytes.Select(b => $"{b:x2}");
            var hexName = string.Join('-', hexArray);

            return hexName;
        }

        public static bool ValidateTagName(string tagDisplayName)
        {
            if (string.IsNullOrWhiteSpace(tagDisplayName)) return false;

            // Regex performance best practice
            // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices

            const string pattern = @"^[a-zA-Z 0-9\.\-\+\#\s]*$";
            var isEng = Regex.IsMatch(tagDisplayName, pattern);
            if (isEng) return true;

            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions#supported-named-blocks
            const string chsPattern = @"\p{IsCJKUnifiedIdeographs}";
            var isChs = Regex.IsMatch(tagDisplayName, chsPattern);

            return isChs;
        }
    }
}
