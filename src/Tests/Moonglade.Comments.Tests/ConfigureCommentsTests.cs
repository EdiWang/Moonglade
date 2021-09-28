using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Comments.Moderators;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moonglade.Comments.Tests
{
    [TestFixture]
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

        [Test]
        public void AddComments_UnknownProvider()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                {"CommentModerator:Provider", "icu"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();

            Assert.Throws<NotSupportedException>(() =>
            {
                services.AddComments(configuration);
            });
        }

        [TestCase("Local")]
        [TestCase("Azure")]
        public void AddComments_KnownProvider(string provider)
        {
            var myConfiguration = new Dictionary<string, string>
            {
                { "CommentModerator:Provider", provider },
                { "CommentModerator:AzureContentModeratorSettings:Endpoint", "https://996.icu" },
                { "CommentModerator:AzureContentModeratorSettings:OcpApimSubscriptionKey", "996" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddComments(configuration);

            var obj = services.FirstOrDefault(p => p.ServiceType == typeof(ICommentModerator));
            Assert.IsNotNull(obj);
        }
    }
}
