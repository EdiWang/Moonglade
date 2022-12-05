using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting.Exporters;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Exporting;

public record ExportTagsDataCommand : IRequest<ExportResult>;

public class ExportTagsDataCommandHandler : IRequestHandler<ExportTagsDataCommand, ExportResult>
{
    private readonly IRepository<TagEntity> _repo;
    public ExportTagsDataCommandHandler(IRepository<TagEntity> repo) => _repo = repo;

    public Task<ExportResult> Handle(ExportTagsDataCommand request, CancellationToken ct)
    {
        var tagExp = new CSVExporter<TagEntity>(_repo, "moonglade-tags", ExportManager.DataDir);
        return tagExp.ExportData(p => new
        {
            p.Id,
            p.NormalizedName,
            p.DisplayName
        }, ct);
    }
}