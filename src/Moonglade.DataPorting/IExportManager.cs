using System.Threading.Tasks;

namespace Moonglade.DataPorting
{
    public interface IExportManager
    {
        Task<string> ExportAsJson(ExportDataType dataType);
    }
}
