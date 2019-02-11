using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Moonglade.ImageStorage.AzureBlob
{
    public class AzureStorageInfo
    {
        public string ConnectionString { get; }

        public string ContainerName { get; }

        public AzureStorageInfo(string connectionString, string containerName)
        {
            ConnectionString = connectionString;
            ContainerName = containerName;
        }
    }
}
