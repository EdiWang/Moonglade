using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class LocalAccountServiceTests
    {
        [TestCase("", ExpectedResult = "")]
        [TestCase(null, ExpectedResult = "")]
        [TestCase(" ", ExpectedResult = "")]
        [TestCase("admin123", ExpectedResult = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=")]
        public string TestHashPassword(string plainMessage)
        {
            return LocalAccountService.HashPassword(plainMessage);
        }
    }
}
