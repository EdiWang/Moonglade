using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Porting.Exporters;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Data.Porting
{
    public class ExportManager : IExportManager
    {
        private readonly IRepository<PostEntity> _postRepository;
        private readonly IMediator _mediator;

        public static readonly string DataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();

        public ExportManager(
            IRepository<PostEntity> postRepository,
            IMediator mediator)
        {
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

        public Task<ExportResult> ExportData(ExportDataType dataType, CancellationToken cancellationToken)
        {
            switch (dataType)
            {
                case ExportDataType.Tags:
                    return _mediator.Send(new ExportTagsDataCommand(), cancellationToken);

                case ExportDataType.Categories:
                    return _mediator.Send(new ExportCategoryDataCommand(), cancellationToken);

                case ExportDataType.FriendLinks:
                    return _mediator.Send(new ExportLinkDataCommand(), cancellationToken);

                case ExportDataType.Pages:
                    return _mediator.Send(new ExportPageDataCommand(), cancellationToken);

                case ExportDataType.Posts:
                    var poExp = new ZippedJsonExporter<PostEntity>(_postRepository, "moonglade-posts", DataDir);
                    var poExportData = poExp.ExportData(p => new
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
