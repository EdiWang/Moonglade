using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Moonglade.Notification.Client.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionTests
    {
        [Test]
        public void AddNotificationClient_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddNotificationClient();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IBlogNotificationClient));
            Assert.IsNotNull(obj1);
        }
    }
}
