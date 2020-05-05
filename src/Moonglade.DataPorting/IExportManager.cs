using System.Threading.Tasks;

namespace Moonglade.DataPorting
{
    public interface IExportManager
    {
        Task<ExportResult> ExportAsJson(ExportDataType dataType);
    }
}
