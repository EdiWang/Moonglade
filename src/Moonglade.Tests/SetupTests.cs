using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Setup;
using NUnit.Framework;

namespace Moonglade.Tests
{
    public class SetupTests
    {
        [Test]
        public void TestGetDatabaseSchemaScript()
        {
            var sql = SetupHelper.GetDatabaseSchemaScript();
            Assert.IsTrue(sql.StartsWith("SET ANSI_NULLS ON"));
        }

        // Only test in local because CI server don't have MSSQL installed locally.
#if DEBUG
        [Test]
        public void TestValidateDatabaseConnection()
        {
            var conn = "Server=(local);Database=moonglade-dev;Trusted_Connection=True;";
            bool ok = SetupHelper.TestDatabaseConnection(conn);
            Assert.IsTrue(ok);
        }

        [Test]
        public void TestSetupDatabase()
        {
            var conn = "Server=(local);Database=moonglade-setup;Trusted_Connection=True;";
            var response = SetupHelper.SetupDatabase(conn);
            Assert.IsTrue(response.IsSuccess);
        }
#endif
    }
}
