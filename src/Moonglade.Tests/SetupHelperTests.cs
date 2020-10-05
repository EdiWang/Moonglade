using Moonglade.Setup;
using NUnit.Framework;
using System;

namespace Moonglade.Tests
{
    [TestFixture]
    public class SetupHelperTests
    {
        [Test]
        public void TestInstanceCreationInvalidConnectionString()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var helper = new SetupRunner(null);
            });

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var helper = new SetupRunner(string.Empty);
            });

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var helper = new SetupRunner(" ");
            });
        }

        [Test]
        public void TestInstanceCreationGoodConnectionString()
        {
            const string connStr = "Server=(localdb)\\MSSQLLocalDB;Database=moonglade-dev;Trusted_Connection=True;";
            var helper = new SetupRunner(connStr);
            Assert.IsTrue(helper.DatabaseConnectionString == connStr);
        }
    }
}
