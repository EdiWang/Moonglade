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

        public async Task<ExportResult> Handle(ExportLinkDataCommand request, CancellationToken cancellationToken)
        {
            var fdExp = new CSVExporter<FriendLinkEntity>(_friendlinkRepository, "moonglade-friendlinks", ExportManager.DataDir);
            var fdExportData = await fdExp.ExportData(p => p, cancellationToken);
            return fdExportData;
        }
    }
}
