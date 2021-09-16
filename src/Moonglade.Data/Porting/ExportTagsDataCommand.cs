using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Porting.Exporters;

namespace Moonglade.Data.Porting
{
    public class ExportTagsDataCommand : IRequest<ExportResult>
    {
    }

    public class ExportTagsDataCommandHandler : IRequestHandler<ExportTagsDataCommand, ExportResult>
    {
        private readonly IRepository<TagEntity> _tagRepository;

        public ExportTagsDataCommandHandler(IRepository<TagEntity> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<ExportResult> Handle(ExportTagsDataCommand request, CancellationToken cancellationToken)
        {
            var tagExp = new CSVExporter<TagEntity>(_tagRepository, "moonglade-tags", ExportManager.DataDir);
            var tagExportData = await tagExp.ExportData(p => new
            {
                p.Id,
                p.NormalizedName,
                p.DisplayName
            }, cancellationToken);
            return tagExportData;
        }
    }
}
