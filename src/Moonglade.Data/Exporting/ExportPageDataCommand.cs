using LiteBus.Commands.Abstractions;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Exporting;

public record ExportPageDataCommand : ICommand<ExportResult>;

public class ExportPageDataCommandHandler(IRepositoryBase<PageEntity> repo) : ICommandHandler<ExportPageDataCommand, ExportResult>
{
    public Task<ExportResult> HandleAsync(ExportPageDataCommand request, CancellationToken ct)
    {
        var pgExp = new ZippedJsonExporter<PageEntity>(repo, "moonglade-pages", Path.GetTempPath());
        return pgExp.ExportData<PageEntity>(null, ct);
    }
}