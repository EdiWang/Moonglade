using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface ITagService
    {
        Task<OperationCode> UpdateAsync(int tagId, string newName);
    }

    public class TagService : ITagService
    {
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IBlogAudit _audit;
        private readonly IDictionary<string, string> _tagNormalizationDictionary;

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
    }
}
