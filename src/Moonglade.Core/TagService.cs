using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface ITagService
    {
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

        public async Task<OperationCode> UpdateAsync(int tagId, string newName)
        {
            var tag = await _tagRepo.GetAsync(tagId);
            if (null == tag) return OperationCode.ObjectNotFound;

            tag.DisplayName = newName;
            tag.NormalizedName = Tag.NormalizeName(newName, _tagNormalizationDictionary);
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
    }
}
