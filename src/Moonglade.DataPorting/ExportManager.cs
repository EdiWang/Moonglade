using System;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.DataPorting
{
    public class ExportManager : IExportManager
    {
        // TODO: Can these be automated into IExporter types with DI?

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
                    var pgExp = new ZippedJsonExporter<CustomPageEntity>(_pageRepository, "moonglade-pages");
                    var pgExportData = await pgExp.ExportData(p => new
                    {
                        p.Title,
                        p.CreateOnUtc,
                        p.CssContent,
                        p.HideSidebar,
                        p.HtmlContent,
                        p.RouteName,
                        p.UpdatedOnUtc
                    });

                    return pgExportData;
                case ExportDataType.Posts:
                    // TODO: Complete this
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }

            return null;
        }
    }
}
