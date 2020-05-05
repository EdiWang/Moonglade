using System.Threading.Tasks;

namespace Moonglade.DataPorting
{
    public interface IExportManager
    {
        Task<ExportResult> ExportData(ExportDataType dataType);
    }
}
