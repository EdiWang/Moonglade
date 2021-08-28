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

            var obj4 = services.FirstOrDefault(p => p.ServiceType == typeof(IPostQueryService));
            Assert.IsNotNull(obj4);
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
