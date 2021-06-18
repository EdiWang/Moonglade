using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Moonglade.Auth.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        private static IConfiguration GetConfiguration(string json)
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
            var configuration = builder.Build();
            return configuration;
        }

        [Test]
        public void AddBlogAuthenticaton_NotSupportedProvider()
        {
            var json = @"{""Authentication"":{""Provider"": ""996""}}";
            var config = GetConfiguration(json);
            IServiceCollection services = new ServiceCollection();

            Assert.Throws<NotSupportedException>(() =>
            {
                services.AddBlogAuthenticaton(config);
            });
        }

        [Test]
        public void AddBlogAuthenticaton_None()
        {
            var json = @"{""Authentication"":{""Provider"": ""None""}}";
            var config = GetConfiguration(json);
            IServiceCollection services = new ServiceCollection();
            services.AddBlogAuthenticaton(config);

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IGetApiKeyQuery));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(ILocalAccountService));
            Assert.IsNotNull(obj2);
        }
    }
}
