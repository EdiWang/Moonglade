using Dapper;
using Moonglade.Data.SqlServer.Setup;
using Moq;
using Moq.Dapper;
using NUnit.Framework;
using System.Data;

namespace Moonglade.Data.SqlServer.Tests
{
    [TestFixture]
    public class SetupRunnerForSqlServerTests
    {
        private MockRepository _mockRepository;

        private Mock<IDbConnection> _mockDbConnection;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockDbConnection = _mockRepository.Create<IDbConnection>();
        }

        [Test]
        public void IsFirstRun_Yes()
        {
            _mockDbConnection.SetupDapper(c => c.ExecuteScalar<int>(It.IsAny<string>(), null, null, null, null)).Returns(0);
            var setupHelper = new SqlServerSetupRunner(_mockDbConnection.Object);

            var result = setupHelper.IsFirstRun();
            Assert.IsTrue(result);
        }

        [Test]
        public void IsFirstRun_No()
        {
            _mockDbConnection.SetupDapper(c => c.ExecuteScalar<int>(It.IsAny<string>(), null, null, null, null)).Returns(1);
            var setupHelper = new SqlServerSetupRunner(_mockDbConnection.Object);

            var result = setupHelper.IsFirstRun();
            Assert.IsFalse(result);
        }
    }
}