using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Data.Porting
{
    public interface IExportManager
    {
        Task<ExportResult> ExportData(ExportDataType dataType, CancellationToken cancellationToken);
    }
}
