using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting.Exporters;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Exporting;

public class ExportLinkDataCommand : IRequest<ExportResult>
{
}

public class ExportLinkDataCommandHandler : IRequestHandler<ExportLinkDataCommand, ExportResult>
{
    private readonly IRepository<FriendLinkEntity> _friendlinkRepository;

    public ExportLinkDataCommandHandler(IRepository<FriendLinkEntity> friendlinkRepository)
    {
        _friendlinkRepository = friendlinkRepository;
    }

    public Task<ExportResult> Handle(ExportLinkDataCommand request, CancellationToken cancellationToken)
    {
        var fdExp = new CSVExporter<FriendLinkEntity>(_friendlinkRepository, "moonglade-friendlinks", ExportManager.DataDir);
        return fdExp.ExportData(p => p, cancellationToken);
    }
}