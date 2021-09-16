using MediatR;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Data.Porting
{
    public class ExportManager : IExportManager
    {
        private readonly IMediator _mediator;

        public static readonly string DataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();

        public ExportManager(IMediator mediator)
        {
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

        public Task<ExportResult> ExportData(ExportType type, CancellationToken cancellationToken)
        {
            return type switch
            {
                ExportType.Tags => _mediator.Send(new ExportTagsDataCommand(), cancellationToken),
                ExportType.Categories => _mediator.Send(new ExportCategoryDataCommand(), cancellationToken),
                ExportType.FriendLinks => _mediator.Send(new ExportLinkDataCommand(), cancellationToken),
                ExportType.Pages => _mediator.Send(new ExportPageDataCommand(), cancellationToken),
                ExportType.Posts => _mediator.Send(new ExportPostDataCommand(), cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
