using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Linq;

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
        }
    }
}
