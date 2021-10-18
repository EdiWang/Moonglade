using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Moonglade.Syndication.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddSyndication_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddSyndication();

        // Assert
        var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(ISyndicationDataSource));
        Assert.IsNotNull(obj1);
    }
}