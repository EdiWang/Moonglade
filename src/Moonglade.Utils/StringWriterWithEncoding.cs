using System.IO;
using System.Text;

namespace Moonglade.Utils
{
    public class StringWriterWithEncoding : StringWriter
    {
        public StringWriterWithEncoding(Encoding encoding)
        {
            Encoding = encoding;
        }

        public override Encoding Encoding { get; }
    }
}
