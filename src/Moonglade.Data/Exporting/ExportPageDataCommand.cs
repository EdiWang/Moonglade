using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting.Exporters;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Exporting;

public record ExportPageDataCommand : IRequest<ExportResult>;

public class ExportPageDataCommandHandler(IRepository<PageEntity> repo) : IRequestHandler<ExportPageDataCommand, ExportResult>
{
    public Task<ExportResult> Handle(ExportPageDataCommand request, CancellationToken ct)
    {
        var pgExp = new ZippedJsonExporter<PageEntity>(repo, "moonglade-pages", ExportManager.DataDir);
        return pgExp.ExportData(p => new
        {
            p.Id,
            p.Title,
            p.Slug,
            p.MetaDescription,
            p.HtmlContent,
            p.CssId,
            p.HideSidebar,
            p.IsPublished,
            p.CreateTimeUtc,
            p.UpdateTimeUtc
        }, ct);
    }
}