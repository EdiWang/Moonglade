using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Porting.Exporters;

namespace Moonglade.Data.Porting
{
    public class ExportCategoryDataCommand : IRequest<ExportResult>
    {
    }

    public class ExportCategoryDataCommandHandler : IRequestHandler<ExportCategoryDataCommand, ExportResult>
    {
        private readonly IRepository<CategoryEntity> _catRepository;

        public ExportCategoryDataCommandHandler(IRepository<CategoryEntity> catRepository)
        {
            _catRepository = catRepository;
        }

        public Task<ExportResult> Handle(ExportCategoryDataCommand request, CancellationToken cancellationToken)
        {
            var catExp = new CSVExporter<CategoryEntity>(_catRepository, "moonglade-categories", ExportManager.DataDir);
            return catExp.ExportData(p => new
            {
                p.Id,
                p.DisplayName,
                p.RouteName,
                p.Note
            }, cancellationToken);
        }
    }
}
