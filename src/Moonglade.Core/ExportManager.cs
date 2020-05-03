using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class ExportManager : IExportManager
    {
        private readonly IRepository<TagEntity> _tagRepository;

        public ExportManager(IRepository<TagEntity> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<string> ExportTagsAsJson()
        {
            var list = await _tagRepository.SelectAsync(tg => new
            {
                NormalizedTagName = tg.NormalizedName,
                TagName = tg.DisplayName
            });

            var json = JsonSerializer.Serialize(list);
            return json;
        }
    }
}
