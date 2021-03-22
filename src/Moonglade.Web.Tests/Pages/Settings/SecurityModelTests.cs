using System.Diagnostics.CodeAnalysis;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Pages.Settings;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Settings
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SecurityModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private SecurityModel CreateSecurityModel()
        {
            return new(
                _mockBlogConfig.Object);
        }

        [Test]
        public void OnGet_StateUnderTest_ExpectedBehavior()
        {
            _mockBlogConfig.Setup(p => p.SecuritySettings).Returns(new SecuritySettings());
            var securityModel = CreateSecurityModel();

            securityModel.OnGet();

            Assert.IsNotNull(securityModel.ViewModel);
        }
    }
}
