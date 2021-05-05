using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.Providers;
using Moonglade.Pingback;
using Moonglade.Web.Configuration;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Configuration
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
    }
}