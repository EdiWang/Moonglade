using LiteBus.Commands.Abstractions;

namespace Moonglade.Data.Exporting;

public record ExportPageDataCommand : ICommand<ExportResult>;

public class ExportPageDataCommandHandler(BlogDbContext db) : ICommandHandler<ExportPageDataCommand, ExportResult>
{
    public async Task<ExportResult> HandleAsync(ExportPageDataCommand request, CancellationToken ct)
    {
        var data = await db.BlogPage.AsNoTracking().ToListAsync(ct);

        var exporter = new ZippedJsonExporter("moonglade-pages", Path.GetTempPath());
        return await exporter.ExportData(data, ct);
    }
}