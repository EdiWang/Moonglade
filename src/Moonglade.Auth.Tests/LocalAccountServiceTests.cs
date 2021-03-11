using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Moonglade.Auth.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class LocalAccountServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<LocalAccountEntity>> _mockLocalAccountRepository;
        private Mock<IBlogAudit> _mockBlogAudit;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLocalAccountRepository = _mockRepository.Create<IRepository<LocalAccountEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
        }

        private LocalAccountService CreateService()
        {
            return new(
                _mockLocalAccountRepository.Object,
                _mockBlogAudit.Object);
        }

        [TestCase("", ExpectedResult = "")]
        [TestCase(null, ExpectedResult = "")]
        [TestCase(" ", ExpectedResult = "")]
        [TestCase("admin123", ExpectedResult = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=")]
        public string HashPassword(string plainMessage)
        {
            return LocalAccountService.HashPassword(plainMessage);
        }
    }
}
