using MediatR;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Exporting;

public record ExportPageDataCommand : IRequest<ExportResult>;

public class ExportPageDataCommandHandler(MoongladeRepository<PageEntity> repo) : IRequestHandler<ExportPageDataCommand, ExportResult>
{
    public Task<ExportResult> Handle(ExportPageDataCommand request, CancellationToken ct)
    {
        var pgExp = new ZippedJsonExporter<PageEntity>(repo, "moonglade-pages", Path.GetTempPath());
        return pgExp.ExportData(p => p, ct);
    }
}