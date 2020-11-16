using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class LocalAccountServiceTests
    {
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
