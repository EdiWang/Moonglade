using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core
{
    public enum ExportDataType
    {
        Tags,
        Categories,
        FriendLinks,
        Pingbacks
    }

    public class ExportManager : IExportManager
    {
        private readonly IRepository<TagEntity> _tagRepository;
        private readonly IRepository<CategoryEntity> _catRepository;
        private readonly IRepository<FriendLinkEntity> _friendlinkRepository;
        private readonly IRepository<PingbackHistoryEntity> _pingbackRepository;

        public ExportManager(
            IRepository<TagEntity> tagRepository,
            IRepository<CategoryEntity> catRepository,
            IRepository<FriendLinkEntity> friendlinkRepository,
            IRepository<PingbackHistoryEntity> pingbackRepository)
        {
            _tagRepository = tagRepository;
            _catRepository = catRepository;
            _friendlinkRepository = friendlinkRepository;
            _pingbackRepository = pingbackRepository;
        }

        public async Task<string> ExportAsJson(ExportDataType dataType)
        {
            switch (dataType)
            {
                case ExportDataType.Tags:
                    var tags = await _tagRepository.SelectAsync(tg => new
                    {
                        NormalizedTagName = tg.NormalizedName,
                        TagName = tg.DisplayName
                    });
                    return List2Json(tags);
                case ExportDataType.Categories:
                    var cats = await _catRepository.SelectAsync(c => new
                    {
                        DisplayName = c.DisplayName,
                        Route = c.Title,
                        Note = c.Note
                    });
                    return List2Json(cats);
                case ExportDataType.FriendLinks:
                    var links = await _friendlinkRepository.SelectAsync(p => new
                    {
                        p.Title,
                        p.LinkUrl
                    });
                    return List2Json(links);
                case ExportDataType.Pingbacks:
                    var pbs = await _pingbackRepository.SelectAsync(p => new
                    {
                        p.Domain,
                        p.PingTimeUtc,
                        p.SourceIp,
                        p.SourceTitle,
                        p.SourceUrl,
                        p.TargetPostTitle
                    });
                    return List2Json(pbs);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        private static string List2Json<T>(IEnumerable<T> list) where T : class
        {
            var json = JsonSerializer.Serialize(list);
            return json;
        }
    }
}
