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
                    var tagExp = new SingeJsonExporter<TagEntity>(_tagRepository);
                    var tagExportData = await tagExp.ExportData(p => new
                    {
                        NormalizedTagName = p.NormalizedName,
                        TagName = p.DisplayName
                    });
                    return tagExportData;

                case ExportDataType.Categories:
                    var catExp = new SingeJsonExporter<CategoryEntity>(_catRepository);
                    var catExportData = await catExp.ExportData(p => new
                    {
                        p.DisplayName,
                        Route = p.Title,
                        p.Note
                    });
                    return catExportData;

                case ExportDataType.FriendLinks:
                    var fdExp = new SingeJsonExporter<FriendLinkEntity>(_friendlinkRepository);
                    var fdExportData = await fdExp.ExportData(p => new
                    {
                        p.Title,
                        p.LinkUrl
                    });
                    return fdExportData;

                case ExportDataType.Pingbacks:
                    var pbExp = new SingeJsonExporter<PingbackHistoryEntity>(_pingbackRepository);
                    var pbExportData = await pbExp.ExportData(p => new
                    {
                        p.Domain,
                        p.PingTimeUtc,
                        p.SourceIp,
                        p.SourceTitle,
                        p.SourceUrl,
                        p.TargetPostTitle
                    });
                    return pbExportData;

                case ExportDataType.Pages:
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

                    var result = await ToZippedJsonResult(pages, "moonglade-pages");
                    return result;
                case ExportDataType.Posts:
                    // TODO: Zip json files
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }

            return null;
        }

        private static async Task<ExportResult> ToZippedJsonResult<T>(IEnumerable<T> list, string fileNamePrefix) where T : class
        {
            var tempId = Guid.NewGuid().ToString();
            string exportDirectory = CreateExportDirectory(tempId);
            foreach (var item in list)
            {
                var json = JsonSerializer.Serialize(item);
                await SaveJsonToDirectory(json, Path.Join(exportDirectory, tempId), $"{Guid.NewGuid()}.json");
            }

            var distPath = Path.Join(exportDirectory, $"{fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
            ZipFile.CreateFromDirectory(Path.Join(exportDirectory, tempId), distPath);

            return new ExportResult
            {
                ExportFormat = ExportFormat.ZippedJsonFiles,
                ZipFilePath = distPath
            };
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
    }
}
