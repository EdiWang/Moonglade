using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage.Providers;
using NUnit.Framework;

namespace Moonglade.ImageStorage.Tests
{
    [TestFixture]
    public class ConfigureImageStorageTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void AddImageStorage_EmptySettingsProvider(string provider)
        {
            var myConfiguration = new Dictionary<string, string>
            {
                { "ImageStorage:Provider", provider }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.AddImageStorage(configuration, options => { });
            });
        }

        [Test]
        public void AddImageStorage_AzureStorage()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                { "ImageStorage:Provider", "azurestorage" },
                { "ImageStorage:AzureStorageSettings:ConnectionString", "DefaultEndpointsProtocol=https;AccountName=ediwangstorage;AccountKey=996;EndpointSuffix=core.windows.net" },
                { "ImageStorage:ContainerName", "ediwang-images" },
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();
            services.AddImageStorage(configuration, options => { });

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IBlogImageStorage));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(AzureBlobConfiguration));
            Assert.IsNotNull(obj2);
        }

        [Test]
        public void AddImageStorage_FileSystem()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                { "ImageStorage:Provider", "filesystem" },
                { "ImageStorage:FileSystemSettings:Path", Path.Combine(Path.GetTempPath(), "images") }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();
            services.AddImageStorage(configuration, options => { });

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IBlogImageStorage));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(FileSystemImageConfiguration));
            Assert.IsNotNull(obj2);
        }

        [Test]
        public void AddImageStorage_MinioStorage()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                { "ImageStorage:Provider", "miniostorage" },
                { "ImageStorage:MinioStorageSettings:EndPoint", "" },
                { "ImageStorage:MinioStorageSettings:AccessKey", "" },
                { "ImageStorage:MinioStorageSettings:SecretKey", "" },
                { "ImageStorage:MinioStorageSettings:BucketName", "" },
                { "ImageStorage:MinioStorageSettings:WithSSL", "true" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();
            services.AddImageStorage(configuration, options => { });

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IBlogImageStorage));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(MinioBlobConfiguration));
            Assert.IsNotNull(obj2);
        }

        [Test]
        public void AddImageStorage_UnknownProvider()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                { "ImageStorage:Provider", "fubao" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();

            Assert.Throws<NotSupportedException>(() =>
            {
                services.AddImageStorage(configuration, options => { });
            });
        }
    }
}