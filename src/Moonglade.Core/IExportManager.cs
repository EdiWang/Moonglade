using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Moonglade.Core
{
    public interface IExportManager
    {
        Task<string> ExportAsJson(ExportDataType dataType);
    }
}
