using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting.Exporters;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Exporting;

public record ExportLinkDataCommand : IRequest<ExportResult>;

public class ExportLinkDataCommandHandler : IRequestHandler<ExportLinkDataCommand, ExportResult>
{
    private readonly IRepository<FriendLinkEntity> _repo;
    public ExportLinkDataCommandHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task<ExportResult> Handle(ExportLinkDataCommand request, CancellationToken ct)
    {
        var fdExp = new CSVExporter<FriendLinkEntity>(_repo, "moonglade-friendlinks", ExportManager.DataDir);
        return fdExp.ExportData(p => p, ct);
    }
}