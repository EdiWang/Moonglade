using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.DataPorting
{
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

        public async Task<ExportResult> ExportData(ExportDataType dataType)
        {
            switch (dataType)
            {
                case ExportDataType.Tags:
                    var tags = await _tagRepository.SelectAsync(tg => new
                    {
                        NormalizedTagName = tg.NormalizedName,
                        TagName = tg.DisplayName
                    });
                    return ToSingleJsonResult(tags);
                case ExportDataType.Categories:
                    var cats = await _catRepository.SelectAsync(c => new
                    {
                        c.DisplayName,
                        Route = c.Title,
                        c.Note
                    });
                    return ToSingleJsonResult(cats);
                case ExportDataType.FriendLinks:
                    var links = await _friendlinkRepository.SelectAsync(p => new
                    {
                        p.Title,
                        p.LinkUrl
                    });
                    return ToSingleJsonResult(links);
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
                    return ToSingleJsonResult(pbs);
                case ExportDataType.Pages:
                    // TODO: Zip json files
                    CreateExportDirectory("pages");
                    break;
                case ExportDataType.Posts:
                    // TODO: Zip json files
                    CreateExportDirectory("posts");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }

            return null;
        }

        private static void CreateExportDirectory(string subDirName)
        {
            var dataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();
            if (null != dataDir)
            {
                var path = Path.Join(dataDir, "export", subDirName);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path);
                }

                Directory.CreateDirectory(path);
            }
        }

        private static string List2Json<T>(IEnumerable<T> list) where T : class
        {
            var json = JsonSerializer.Serialize(list);
            return json;
        }

        private static ExportResult ToSingleJsonResult<T>(IEnumerable<T> list) where T : class
        {
            return new ExportResult
            {
                ExportFormat = ExportFormat.SingleJsonFile,
                JsonContent = List2Json(list)
            };
        }
    }
}
