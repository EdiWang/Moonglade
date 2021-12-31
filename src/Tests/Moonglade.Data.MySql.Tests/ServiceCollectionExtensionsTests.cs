using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.Infrastructure;
using NUnit.Framework;
using System.Data;
using System.Linq;

namespace Moonglade.Data.MySql.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddCoreBloggingServices_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddMySqlStorage(@"Server=localhost;Port=6612;Database=moonglade;Uid=root;Pwd=pzy@2021;");

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IDbConnection));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(IRepository<>));
            Assert.IsNotNull(obj2);

            var obj3 = services.FirstOrDefault(p => p.ServiceType == typeof(BlogMySqlDbContext));
            Assert.IsNotNull(obj3);
        }
    }
}