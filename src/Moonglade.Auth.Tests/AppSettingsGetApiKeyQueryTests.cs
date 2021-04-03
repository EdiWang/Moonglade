using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Moonglade.Auth.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AppSettingsGetApiKeyQueryTests
    {
        private MockRepository _mockRepository;
        private Mock<IOptions<AuthenticationSettings>> _mockOptions;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _mockOptions = _mockRepository.Create<IOptions<AuthenticationSettings>>();
        }

        private AppSettingsGetApiKeyQuery CreateAppSettingsGetApiKeyQuery()
        {
            return new(_mockOptions.Object);
        }

        [Test]
        public async Task Execute_ExpectedBehavior()
        {
            _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.AzureAD,
                ApiKeys = new ApiKey[]
                {
                    new () { Id = 1, Key = "fuck996", Owner = "996fucker" },
                    new () { Id = 2, Key = "pdd007", Owner = "gotoicu" },
                    new () { Id = 3, Key = "251", Owner = "hwaiguo" }
                }
            });

            var appSettingsGetApiKeyQuery = CreateAppSettingsGetApiKeyQuery();
            string providedApiKey = "251";

            var result = await appSettingsGetApiKeyQuery.Execute(providedApiKey);

            Assert.AreEqual("251", result.Key);
        }
    }
}
