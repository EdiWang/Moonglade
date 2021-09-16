using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Porting.Exporters;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Moonglade.Data.Porting
{
    public class ExportManager : IExportManager
    {
        private readonly IRepository<CategoryEntity> _catRepository;
        private readonly IRepository<FriendLinkEntity> _friendlinkRepository;
        private readonly IRepository<PageEntity> _pageRepository;
        private readonly IRepository<PostEntity> _postRepository;
        private readonly IMediator _mediator;

        public static readonly string DataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();

        public ExportManager(
            IRepository<CategoryEntity> catRepository,
            IRepository<FriendLinkEntity> friendlinkRepository,
            IRepository<PageEntity> pageRepository,
            IRepository<PostEntity> postRepository, 
            IMediator mediator)
        {
            _catRepository = catRepository;
            _friendlinkRepository = friendlinkRepository;
            _pageRepository = pageRepository;
            _postRepository = postRepository;
            _mediator = mediator;
        }

        public static string CreateExportDirectory(string directory, string subDirName)
        {
            if (directory is null) return null;

            var path = Path.Join(directory, "export", subDirName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }

            Directory.CreateDirectory(path);
            return path;
        }

        public async Task<ExportResult> ExportData(ExportDataType dataType, CancellationToken cancellationToken)
        {
            switch (dataType)
            {
                case ExportDataType.Tags:
                    var tagExportData = await _mediator.Send(new ExportTagsDataCommand(), cancellationToken);
                    return tagExportData;

                case ExportDataType.Categories:
                    var catExp = new CSVExporter<CategoryEntity>(_catRepository, "moonglade-categories", DataDir);
                    var catExportData = await catExp.ExportData(p => new
                    {
                        p.Id,
                        p.DisplayName,
                        p.RouteName,
                        p.Note
                    }, cancellationToken);
                    return catExportData;

                case ExportDataType.FriendLinks:
                    var fdExp = new CSVExporter<FriendLinkEntity>(_friendlinkRepository, "moonglade-friendlinks", DataDir);
                    var fdExportData = await fdExp.ExportData(p => p, cancellationToken);
                    return fdExportData;

                case ExportDataType.Pages:
                    var pgExp = new ZippedJsonExporter<PageEntity>(_pageRepository, "moonglade-pages", DataDir);
                    var pgExportData = await pgExp.ExportData(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Slug,
                        p.MetaDescription,
                        p.HtmlContent,
                        p.CssContent,
                        p.HideSidebar,
                        p.IsPublished,
                        p.CreateTimeUtc,
                        p.UpdateTimeUtc
                    }, cancellationToken);

                    return pgExportData;
                case ExportDataType.Posts:
                    var poExp = new ZippedJsonExporter<PostEntity>(_postRepository, "moonglade-posts", DataDir);
                    var poExportData = await poExp.ExportData(p => new
                    {
                        p.Title,
                        p.Slug,
                        p.ContentAbstract,
                        p.PostContent,
                        p.CreateTimeUtc,
                        p.CommentEnabled,
                        p.PostExtension.Hits,
                        p.PostExtension.Likes,
                        p.PubDateUtc,
                        p.ContentLanguageCode,
                        p.ExposedToSiteMap,
                        p.IsDeleted,
                        p.IsFeedIncluded,
                        p.IsPublished,
                        Categories = p.PostCategory.Select(pc => pc.Category.DisplayName),
                        Tags = p.Tags.Select(pt => pt.DisplayName)
                    }, cancellationToken);

                    return poExportData;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
    }
}
