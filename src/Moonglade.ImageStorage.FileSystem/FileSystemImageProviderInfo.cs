using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.ImageStorage.FileSystem
{
    public class FileSystemImageProviderInfo
    {
        public string Path { get; set; }

        public FileSystemImageProviderInfo(string path)
        {
            Path = path;
        }
    }
}
