using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                {"ImageStorage:Provider", provider}
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
    }
}