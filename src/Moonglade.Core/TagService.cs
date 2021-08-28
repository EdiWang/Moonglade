using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface ITagService
    {
        Task<Tag> Create(string name);
        Task<OperationCode> UpdateAsync(int tagId, string newName);
        Task<IReadOnlyList<KeyValuePair<Tag, int>>> GetHotTagsAsync(int top);
        Tag Get(string normalizedName);
    }

    public class TagService : ITagService
    {
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IBlogAudit _audit;
        private readonly IDictionary<string, string> _tagNormalizationDictionary;

        private readonly Expression<Func<TagEntity, Tag>> _tagSelector = t => new()
        {
            Id = t.Id,
            NormalizedName = t.NormalizedName,
            DisplayName = t.DisplayName
        };

        public TagService(
            IRepository<TagEntity> tagRepo,
            IBlogAudit audit,
            IConfiguration configuration)
        {
            _tagRepo = tagRepo;
            _audit = audit;

            _tagNormalizationDictionary =
                configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
        }

        public async Task<Tag> Create(string name)
        {
            if (!ValidateTagName(name)) return null;

            var normalizedName = NormalizeTagName(name, _tagNormalizationDictionary);
            if (_tagRepo.Any(t => t.NormalizedName == normalizedName))
            {
                return Get(normalizedName);
            }

            var newTag = new TagEntity
            {
                DisplayName = name,
                NormalizedName = normalizedName
            };

            var tag = await _tagRepo.AddAsync(newTag);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.TagCreated,
                $"Tag '{tag.NormalizedName}' created.");

            return new()
            {
                DisplayName = newTag.DisplayName,
                NormalizedName = newTag.NormalizedName
            };
        }

        public async Task<OperationCode> UpdateAsync(int tagId, string newName)
        {
            var tag = await _tagRepo.GetAsync(tagId);
            if (null == tag) return OperationCode.ObjectNotFound;

            tag.DisplayName = newName;
            tag.NormalizedName = NormalizeTagName(newName, _tagNormalizationDictionary);
            await _tagRepo.UpdateAsync(tag);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.TagUpdated, $"Tag id '{tagId}' is updated.");

            return OperationCode.Done;
        }

        public async Task<IReadOnlyList<KeyValuePair<Tag, int>>> GetHotTagsAsync(int top)
        {
            if (!_tagRepo.Any()) return new List<KeyValuePair<Tag, int>>();

            var spec = new TagSpec(top);
            var tags = await _tagRepo.SelectAsync(spec, t =>
                new KeyValuePair<Tag, int>(new()
                {
                    Id = t.Id,
                    DisplayName = t.DisplayName,
                    NormalizedName = t.NormalizedName
                }, t.Posts.Count));

            return tags;
        }

        public Tag Get(string normalizedName)
        {
            var tag = _tagRepo.SelectFirstOrDefault(new TagSpec(normalizedName), _tagSelector);
            return tag;
        }

        public static string NormalizeTagName(string orgTagName, IDictionary<string, string> normalizations)
        {
            var isEnglishName = Regex.IsMatch(orgTagName, @"^[a-zA-Z 0-9\.\-\+\#\s]*$");
            if (isEnglishName)
            {
                var result = new StringBuilder(orgTagName);
                foreach (var (key, value) in normalizations)
                {
                    result.Replace(key, value);
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
