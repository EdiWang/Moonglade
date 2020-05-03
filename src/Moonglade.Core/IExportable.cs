using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Moonglade.Core
{
    public interface IExportable
    {
        XDocument GetXmlDocument();
    }
}
