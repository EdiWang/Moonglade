using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Web.Configuration;
using Moq;
using NUnit.Framework;

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
        }
    }
}
