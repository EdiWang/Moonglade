using Moonglade.Auth;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Auth.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddBlogAuthenticaton_None()
        {
            var config = @"{""Authentication"":{""Provider"": ""996""}}";
            var builder = new ConfigurationBuilder();
            builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(config)));
            var configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            Assert.Throws<NotSupportedException>(() =>
            {
                services.AddBlogAuthenticaton(configuration);
            });
        }
    }
}
