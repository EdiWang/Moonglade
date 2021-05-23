using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Caching.Filters;
using NUnit.Framework;

namespace Moonglade.Caching.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddBlogCache_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddBlogCache();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IBlogCache));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(ClearSubscriptionCache));
            Assert.IsNotNull(obj2);

            var obj3 = services.FirstOrDefault(p => p.ServiceType == typeof(ClearSiteMapCache));
            Assert.IsNotNull(obj3);
        }
    }
}
