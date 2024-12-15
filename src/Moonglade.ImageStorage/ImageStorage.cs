﻿using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public class ImageStorageSettings
{
    public int CacheMinutes { get; set; }

    public string Provider { get; set; }

    public string FileSystemPath { get; set; }

    public AzureStorageSettings AzureStorageSettings { get; set; }

    public MinioStorageSettings MinioStorageSettings { get; set; }
}