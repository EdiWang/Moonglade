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
        private readonly IRepository<CategoryEntity> _catRepository;

        public ExportManager(
            IRepository<TagEntity> tagRepository, 
            IRepository<CategoryEntity> catRepository)
        {
            _tagRepository = tagRepository;
            _catRepository = catRepository;
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

        public async Task<string> ExportCatsAsJson()
        {
            var list = await _catRepository.SelectAsync(c => new
            {
                DisplayName = c.DisplayName,
                Route = c.Title,
                Note = c.Note
            });

            var json = JsonSerializer.Serialize(list);
            return json;
        }
    }
}
