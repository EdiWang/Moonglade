using Microsoft.Extensions.DependencyInjection;
using Moonglade.Web.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Moonglade.Pingback;

namespace Moonglade.Web.Tests.Configuration
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ConfigurePingbackTests
    {
        [Test]
        public void AddPingback_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPingback();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IPingSourceInspector));
            Assert.IsNotNull(obj1);

            var obj2 = services.FirstOrDefault(p => p.ServiceType == typeof(IPingbackRepository));
            Assert.IsNotNull(obj2);

            var obj3 = services.FirstOrDefault(p => p.ServiceType == typeof(IPingbackSender));
            Assert.IsNotNull(obj3);

            var obj4 = services.FirstOrDefault(p => p.ServiceType == typeof(IPingbackService));
            Assert.IsNotNull(obj4);
        }
    }
}
