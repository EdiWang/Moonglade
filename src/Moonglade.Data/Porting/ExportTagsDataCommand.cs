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

        public Task<ExportResult> Handle(ExportTagsDataCommand request, CancellationToken cancellationToken)
        {
            var tagExp = new CSVExporter<TagEntity>(_tagRepository, "moonglade-tags", ExportManager.DataDir);
            return tagExp.ExportData(p => new
            {
                p.Id,
                p.NormalizedName,
                p.DisplayName
            }, cancellationToken);
        }
    }
}
