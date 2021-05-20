using Moonglade.Page;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Page.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddBlogPage_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            // Act
            services.AddBlogPage();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IBlogPageService));
            Assert.IsNotNull(obj1);
        }
    }
}
