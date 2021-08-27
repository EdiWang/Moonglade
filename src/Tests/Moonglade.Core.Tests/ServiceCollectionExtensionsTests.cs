using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Linq;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddCoreBloggingServices_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddCoreBloggingServices();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IBlogStatistics));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(ICategoryService));
            Assert.IsNotNull(obj2);

            var obj3 = services.FirstOrDefault(p => p.ServiceType == typeof(IPostManageService));
            Assert.IsNotNull(obj3);

            var obj4 = services.FirstOrDefault(p => p.ServiceType == typeof(IPostQueryService));
            Assert.IsNotNull(obj4);

            var obj5 = services.FirstOrDefault(p => p.ServiceType == typeof(ISearchService));
            Assert.IsNotNull(obj5);

            var obj6 = services.FirstOrDefault(p => p.ServiceType == typeof(ITagService));
            Assert.IsNotNull(obj6);
        }

        [Test]
        public void AddReleaseCheckerClient_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddReleaseCheckerClient();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IReleaseCheckerClient));
            Assert.IsNotNull(obj1);
        }
    }
}
