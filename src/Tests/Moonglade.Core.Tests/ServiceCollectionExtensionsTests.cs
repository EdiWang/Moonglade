using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Moonglade.Core.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddReleaseCheckerClient_OK()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddReleaseCheckerClient();

        var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IReleaseCheckerClient));
        Assert.IsNotNull(obj1);
    }
}