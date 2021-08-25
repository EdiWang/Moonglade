using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Linq;

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
