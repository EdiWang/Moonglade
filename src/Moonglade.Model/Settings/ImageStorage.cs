using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Model.Settings
{
    public class ImageStorage
    {
        public string Provider { get; set; }

        public AzureStorageSettings AzureStorageSettings { get; set; }

        public FileSystemSettings FileSystemSettings { get; set; }
    }

    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }

        public string ContainerName { get; set; }
    }

    public class FileSystemSettings
    {
        public string Path { get; set; }
    }
}
