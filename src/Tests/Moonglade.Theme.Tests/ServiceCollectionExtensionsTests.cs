using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Linq;

namespace Moonglade.Theme.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddBlogTheme_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddBlogTheme();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IThemeService));
            Assert.IsNotNull(obj1);
        }
    }
}
