using Moonglade.Web.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Web.Tests.Configuration
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ConfigureCommentsTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void AddComments_EmptySettingsProvider(string provider)
        {
            var myConfiguration = new Dictionary<string, string>
            {
                {"CommentModerator:Provider", provider}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.AddComments(configuration);
            });
        }
    }
}
