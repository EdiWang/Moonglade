using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Data.Infrastructure;
using NUnit.Framework;

namespace Moonglade.Data.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddCoreBloggingServices_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddDataStorage(@"Server=(localdb)\\MSSQLLocalDB;Database=moonglade;Trusted_Connection=True;");

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IDbConnection));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(IRepository<>));
            Assert.IsNotNull(obj2);

            var obj3 = services.FirstOrDefault(p => p.ServiceType == typeof(BlogDbContext));
            Assert.IsNotNull(obj3);
        }
    }
}
