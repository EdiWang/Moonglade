using LiteBus.Commands.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Data.Exporting;

public record ExportPostDataCommand : ICommand<ExportResult>;

public class ExportPostDataCommandHandler(IRepositoryBase<PostEntity> repo) : ICommandHandler<ExportPostDataCommand, ExportResult>
{
    public Task<ExportResult> HandleAsync(ExportPostDataCommand request, CancellationToken ct)
    {
        var exporter = new ZippedJsonExporter<PostEntity>(repo, "moonglade-posts", Path.GetTempPath());
        var spec = new PostEntityToPostExportModelSpec();

        var data = exporter.ExportData(spec, ct);

        return data;
    }
}