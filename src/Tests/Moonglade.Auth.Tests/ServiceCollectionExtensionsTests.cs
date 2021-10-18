using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text;

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
    }
}
