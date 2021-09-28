using AspNetCoreRateLimit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Web.Configuration;
using Moq;
using NUnit.Framework;
using System.Linq;

namespace Moonglade.Web.Tests.Configuration
{
    [TestFixture]
    public class ConfigureRateLimitTests
    {
        [Test]
        public void AddRateLimit_OK()
        {
            var icsMock = new Mock<IConfigurationSection>(MockBehavior.Default);

            IServiceCollection services = new ServiceCollection();
            services.AddRateLimit(icsMock.Object);

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IRateLimitConfiguration));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(IRateLimitCounterStore));
            Assert.IsNotNull(obj2);

            var obj3 = services.FirstOrDefault(p => p.ServiceType == typeof(IIpPolicyStore));
            Assert.IsNotNull(obj3);
        }
    }
}
