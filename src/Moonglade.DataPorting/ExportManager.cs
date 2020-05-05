using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;

namespace Moonglade.DataPorting
{
    // TODO: Redesign this spaghetti code
    public class ExportManager : IExportManager
    {
        private readonly IRepository<TagEntity> _tagRepository;
        private readonly IRepository<CategoryEntity> _catRepository;
        private readonly IRepository<FriendLinkEntity> _friendlinkRepository;
        private readonly IRepository<PingbackHistoryEntity> _pingbackRepository;
        private readonly IRepository<CustomPageEntity> _pageRepository;

        public ExportManager(
            IRepository<TagEntity> tagRepository,
            IRepository<CategoryEntity> catRepository,
            IRepository<FriendLinkEntity> friendlinkRepository,
            IRepository<PingbackHistoryEntity> pingbackRepository,
            IRepository<CustomPageEntity> pageRepository)
        {
            _tagRepository = tagRepository;
            _catRepository = catRepository;
            _friendlinkRepository = friendlinkRepository;
            _pingbackRepository = pingbackRepository;
            _pageRepository = pageRepository;
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
                    string exportDirectory = CreateExportDirectory("pages");
                    var pages = await _pageRepository.SelectAsync(p => new
                    {
                        p.Title,
                        p.CreateOnUtc,
                        p.CssContent,
                        p.HideSidebar,
                        p.HtmlContent,
                        p.RouteName,
                        p.UpdatedOnUtc
                    });
                    foreach (var page in pages)
                    {
                        var json = JsonSerializer.Serialize(page);
                        await SaveJsonToDirectory(json, Path.Join(exportDirectory, "pages"), $"{page.RouteName}.json");
                    }

                    var distPath = Path.Join(exportDirectory, $"moonglade-pages-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
                    ZipFile.CreateFromDirectory(Path.Join(exportDirectory, "pages"), distPath);

                    return new ExportResult
                    {
                        ExportFormat = ExportFormat.ZippedJsonFiles,
                        ZipFilePath = distPath
                    };
                case ExportDataType.Posts:
                    // TODO: Zip json files
                    CreateExportDirectory("posts");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }

            return null;
        }

        private static async Task SaveJsonToDirectory(string json, string directory, string filename)
        {
            var path = Path.Join(directory, filename);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8);
        }

        private static string CreateExportDirectory(string subDirName)
        {
            var dataDir = AppDomain.CurrentDomain.GetData(Constants.DataDirectory)?.ToString();
            if (null != dataDir)
            {
                var path = Path.Join(dataDir, "export", subDirName);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path);
                }

                Directory.CreateDirectory(path);
                return Path.Join(dataDir, "export");
            }

            return null;
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
