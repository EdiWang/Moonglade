using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Setup;
using NUnit.Framework;

namespace Moonglade.Tests
{
    public class SetupTests
    {
        private string _conn;

        [SetUp]
        public void Setup()
        {
            _conn = "Server=(local);Database=moonglade-setup;Trusted_Connection=True;";
#if DEBUG
            SetupHelper.ClearData(_conn);
#endif
        }

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
            bool ok = SetupHelper.TestDatabaseConnection(_conn);
            Assert.IsTrue(ok);
        }

        [Test]
        public void TestSetupDatabase()
        {
            var response = SetupHelper.SetupDatabase(_conn);
            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        public void TestIsFirstRun()
        {
            var response = SetupHelper.IsFirstRun(_conn);
            Assert.IsTrue(response);
        }
#endif
    }
}
